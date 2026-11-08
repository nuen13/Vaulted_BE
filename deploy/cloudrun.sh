#!/usr/bin/env bash
# Build the Vaulted image and deploy it to Google Cloud Run (scales to zero when idle).
#
# Usage:  PROJECT_ID=my-gcp-project ./deploy/cloudrun.sh
# The first run creates the DB secret from deploy/.db-connection (written by deploy/azure-sql.sh)
# or from DB_CONNECTION_STRING if set.
#
# Kept inside Google's always-free limits: at most 1 instance, scales to zero, only the newest image
# is kept in Artifact Registry (0.5 GB free), 2 secrets (6 free). Run deploy/gcp-billing-guard.sh once
# to cut billing off automatically if anything ever does cost money.
#
# Optional: REGION (default us-central1, a free-tier region), SERVICE (default vaulted),
#           VITE_GOOGLE_CLIENT_ID (default: read from Vaulted_FE/.env).
set -euo pipefail

cd "$(dirname "$0")/.."

: "${PROJECT_ID:?Set PROJECT_ID to your Google Cloud project id}"
REGION="${REGION:-us-central1}"
SERVICE="${SERVICE:-vaulted}"
REPO="${REPO:-vaulted}"
ENV_FILE="deploy/cloudrun.env.yaml"
IMAGE="$REGION-docker.pkg.dev/$PROJECT_ID/$REPO/$SERVICE:$(date +%Y%m%d-%H%M%S)"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE — copy deploy/cloudrun.env.example.yaml and fill it in." >&2
  exit 1
fi

if [[ -z "${VITE_GOOGLE_CLIENT_ID:-}" && -f Vaulted_FE/.env ]]; then
  VITE_GOOGLE_CLIENT_ID="$(grep -E '^\s*VITE_GOOGLE_CLIENT_ID\s*=' Vaulted_FE/.env | head -1 | cut -d= -f2- | tr -d ' "')"
fi
: "${VITE_GOOGLE_CLIENT_ID:?Set VITE_GOOGLE_CLIENT_ID (or put it in Vaulted_FE/.env)}"

if [[ -z "${DB_CONNECTION_STRING:-}" && -f deploy/.db-connection ]]; then
  DB_CONNECTION_STRING="$(cat deploy/.db-connection)"
fi

gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Enabling APIs"
gcloud services enable run.googleapis.com artifactregistry.googleapis.com secretmanager.googleapis.com

echo "==> Artifact Registry repo"
gcloud artifacts repositories describe "$REPO" --location "$REGION" >/dev/null 2>&1 ||
  gcloud artifacts repositories create "$REPO" --repository-format docker --location "$REGION"
# Only 0.5 GB of image storage is free: keep just the newest image (the one Cloud Run is serving).
gcloud artifacts repositories set-cleanup-policies "$REPO" --location "$REGION" \
  --policy deploy/artifact-cleanup.json --quiet >/dev/null
gcloud auth configure-docker "$REGION-docker.pkg.dev" --quiet

echo "==> Secrets"
ensure_secret() { # name, value-if-creating
  if ! gcloud secrets describe "$1" >/dev/null 2>&1; then
    [[ -n "$2" ]] || { echo "Secret $1 doesn't exist yet; provide its value (see usage at top)." >&2; exit 1; }
    printf '%s' "$2" | gcloud secrets create "$1" --data-file=- --replication-policy automatic
  fi
}
ensure_secret vaulted-jwt-key "$(openssl rand -base64 48)"
ensure_secret vaulted-db-connection "${DB_CONNECTION_STRING:-}"

# Cloud Run runs as the default compute service account unless told otherwise; let it read the secrets.
PROJECT_NUMBER="$(gcloud projects describe "$PROJECT_ID" --format 'value(projectNumber)')"
RUNTIME_SA="$PROJECT_NUMBER-compute@developer.gserviceaccount.com"
for s in vaulted-jwt-key vaulted-db-connection; do
  gcloud secrets add-iam-policy-binding "$s" \
    --member "serviceAccount:$RUNTIME_SA" --role roles/secretmanager.secretAccessor >/dev/null
done

echo "==> Building $IMAGE"
docker buildx build --platform linux/amd64 \
  --build-arg VITE_GOOGLE_CLIENT_ID="$VITE_GOOGLE_CLIENT_ID" \
  -t "$IMAGE" --push .

echo "==> Deploying $SERVICE"
gcloud run deploy "$SERVICE" \
  --image "$IMAGE" \
  --region "$REGION" \
  --allow-unauthenticated \
  --min-instances 0 \
  --max-instances 1 \
  --cpu 1 --memory 512Mi \
  --env-vars-file "$ENV_FILE" \
  --set-secrets "Auth__JwtKey=vaulted-jwt-key:latest,ConnectionStrings__DefaultConnection=vaulted-db-connection:latest"

URL="$(gcloud run services describe "$SERVICE" --region "$REGION" --format 'value(status.url)')"
echo
echo "Deployed: $URL"
echo "Add $URL to 'Authorized JavaScript origins' of your Google OAuth client, or sign-in will fail."
