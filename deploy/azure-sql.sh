#!/usr/bin/env bash
# Create the Vaulted database on the Azure SQL Database free tier, configured so it can never bill:
#   - free tier (100,000 vCore-seconds, 32 GB data, 32 GB backups per month, for the life of the subscription)
#   - "AutoPause" when the monthly allowance runs out: the database goes offline until the 1st of next
#     month instead of charging. This choice is what guarantees $0 — never switch it to "continue with charges"
#     (Microsoft: that switch is one-way).
#   - locally redundant backups (the only kind included in the free tier)
#   - checks that Microsoft Defender for SQL (a paid plan) is off for the subscription
# Then loads Database/*.sql and writes the connection string to deploy/.db-connection (git-ignored).
#
# Needs: Azure CLI logged in (brew install azure-cli && az login), Docker (to run sqlcmd).
# Usage:  ./deploy/azure-sql.sh
# Optional: LOCATION (default centralus, near Cloud Run's us-central1), RESOURCE_GROUP, SQL_SERVER, DB_NAME.
set -euo pipefail

cd "$(dirname "$0")/.."

LOCATION="${LOCATION:-centralus}"
RG="${RESOURCE_GROUP:-vaulted-rg}"
DB="${DB_NAME:-vaulted}"
ADMIN_USER="${SQL_ADMIN_USER:-vaultedadmin}"
CONN_FILE="deploy/.db-connection"

SUB_ID="$(az account show --query id -o tsv)" || { echo "Run 'az login' first." >&2; exit 1; }
# Server names are global, so make the default unique per subscription.
SERVER="${SQL_SERVER:-vaulted-${SUB_ID:0:8}}"
echo "Subscription: $(az account show --query name -o tsv) ($SUB_ID)"

echo "==> Checking Microsoft Defender for SQL is off (it is a paid plan)"
az provider register --namespace Microsoft.Sql --wait
for plan in SqlServers SqlServerVirtualMachines; do
  tier="$(az security pricing show -n "$plan" --query pricingTier -o tsv 2>/dev/null || echo Unknown)"
  if [[ "$tier" == "Unknown" ]]; then
    echo "    Couldn't read Defender plan '$plan'; check Defender for Cloud > Environment settings shows it Off." >&2
  elif [[ "$tier" != "Free" ]]; then
    echo "Defender plan '$plan' is '$tier' on this subscription and would bill for the SQL server." >&2
    echo "Turn it off first:  az security pricing create -n $plan --tier Free" >&2
    exit 1
  fi
done

echo "==> Resource group $RG ($LOCATION)"
az group create -n "$RG" -l "$LOCATION" -o none

if az sql server show -g "$RG" -n "$SERVER" -o none 2>/dev/null; then
  echo "==> SQL server $SERVER already exists"
  if [[ ! -f "$CONN_FILE" && -z "${SQL_ADMIN_PASSWORD:-}" ]]; then
    echo "No $CONN_FILE and no SQL_ADMIN_PASSWORD; set SQL_ADMIN_PASSWORD to the server's admin password." >&2
    exit 1
  fi
  ADMIN_PASSWORD="${SQL_ADMIN_PASSWORD:-$(sed -E 's/.*Password=([^;]*).*/\1/' "$CONN_FILE")}"
else
  # Random, and meets Azure's complexity rules (upper, lower, digit, symbol).
  ADMIN_PASSWORD="${SQL_ADMIN_PASSWORD:-$(openssl rand -base64 30 | tr -d '/+=' | cut -c1-28)Aa1!}"
  echo "==> Creating SQL server $SERVER (the logical server itself has no charge)"
  az sql server create -g "$RG" -n "$SERVER" -l "$LOCATION" \
    --admin-user "$ADMIN_USER" --admin-password "$ADMIN_PASSWORD" \
    --minimal-tls-version 1.2 -o none
fi

# Cloud Run has no fixed outgoing IP (a fixed one needs Cloud NAT, ~$30/month), so the firewall must
# accept any address. Access still needs the admin password, and connections are encrypted (TLS 1.2+).
echo "==> Firewall: allow any IP (password + TLS still required)"
az sql server firewall-rule create -g "$RG" -s "$SERVER" -n allow-cloud-run \
  --start-ip-address 0.0.0.0 --end-ip-address 255.255.255.255 -o none

if az sql db show -g "$RG" -s "$SERVER" -n "$DB" -o none 2>/dev/null; then
  echo "==> Database $DB already exists"
else
  echo "==> Creating free-tier database $DB"
  az sql db create -g "$RG" -s "$SERVER" -n "$DB" \
    --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless \
    --use-free-limit true --free-limit-exhaustion-behavior AutoPause \
    --backup-storage-redundancy Local \
    --max-size 32GB -o none
fi

echo "==> Verifying the database cannot bill"
read -r FREE BEHAVIOR SKU <<<"$(az sql db show -g "$RG" -s "$SERVER" -n "$DB" \
  --query "[useFreeLimit, freeLimitExhaustionBehavior, currentSku.name]" -o tsv | tr '\n' ' ')"
echo "    useFreeLimit=$FREE  exhaustionBehavior=$BEHAVIOR  sku=$SKU"
if [[ "$FREE" != "true" || "$BEHAVIOR" != "AutoPause" ]]; then
  echo "NOT ZERO-COST: expected useFreeLimit=true and exhaustionBehavior=AutoPause. Fix before using it." >&2
  exit 1
fi

HOST="$SERVER.database.windows.net"
# The free database pauses after 15 idle minutes; the first connection after that waits while it resumes.
# SqlClient retries Azure's "database is resuming" errors on open, so allow it up to ~90 s.
CONN="Server=tcp:$HOST,1433;Database=$DB;User Id=$ADMIN_USER;Password=$ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connect Timeout=90;ConnectRetryCount=6;ConnectRetryInterval=10"
umask 077
printf '%s' "$CONN" > "$CONN_FILE"
echo "==> Connection string saved to $CONN_FILE (git-ignored, readable only by you)"

echo "==> Loading schema and stored procedures"
for f in Schema CategoryStoreProcedure MediaStoreProcedure ReviewStoreProcedure PhotoStoreProcedure \
         QuoteStoreProcedure StatusStoreProcedure UserStoreProcedure; do
  echo "    $f.sql"
  docker run --rm --platform linux/amd64 -v "$PWD/Database:/sql:ro" mcr.microsoft.com/mssql-tools \
    /opt/mssql-tools/bin/sqlcmd -S "tcp:$HOST,1433" -d "$DB" -U "$ADMIN_USER" -P "$ADMIN_PASSWORD" \
    -N -l 90 -b -i "/sql/$f.sql" >/dev/null
done

echo
echo "Done. Deploy the app with:  PROJECT_ID=<gcp project> ./deploy/cloudrun.sh"
echo "It picks up $CONN_FILE automatically."
