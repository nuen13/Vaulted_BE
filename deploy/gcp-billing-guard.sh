#!/usr/bin/env bash
# One-time setup: a 1-unit budget (in the billing account's currency) on the project plus a kill switch that unlinks billing as soon as the
# month's cost reaches $0.01. Google has no hard spending cap; this is the closest thing to one.
#
# What happens if it fires: the project loses billing, Cloud Run stops serving, and Google may
# delete paid resources after a while. The database lives in Azure, and the image can be rebuilt
# with deploy/cloudrun.sh, so nothing important is lost. Re-link billing in the console to restore.
#
# Budget data reaches Google's billing system with a delay of hours, so a few cents can still
# accrue before the switch fires; it stops anything from growing beyond that.
#
# Usage:  PROJECT_ID=my-gcp-project ./deploy/gcp-billing-guard.sh
set -euo pipefail

cd "$(dirname "$0")/.."

: "${PROJECT_ID:?Set PROJECT_ID to your Google Cloud project id}"
REGION="${REGION:-us-central1}"
KILL_AT="${KILL_AT:-0.01}"
TOPIC="billing-alerts"
FUNCTION="vaulted-billing-guard"
GUARD_SA_NAME="billing-guard"
GUARD_SA="$GUARD_SA_NAME@$PROJECT_ID.iam.gserviceaccount.com"

gcloud config set project "$PROJECT_ID" >/dev/null
BILLING_ACCOUNT="$(gcloud billing projects describe "$PROJECT_ID" --format 'value(billingAccountName)' | sed 's#billingAccounts/##')"
[[ -n "$BILLING_ACCOUNT" ]] || { echo "Project has no billing account linked." >&2; exit 1; }
PROJECT_NUMBER="$(gcloud projects describe "$PROJECT_ID" --format 'value(projectNumber)')"
BUILD_SA="$PROJECT_NUMBER-compute@developer.gserviceaccount.com"
CURRENCY="$(gcloud billing accounts describe "$BILLING_ACCOUNT" --format 'value(currencyCode)')"

echo "==> Enabling APIs"
gcloud services enable cloudbilling.googleapis.com billingbudgets.googleapis.com pubsub.googleapis.com \
  cloudfunctions.googleapis.com cloudbuild.googleapis.com eventarc.googleapis.com run.googleapis.com \
  artifactregistry.googleapis.com

echo "==> Pub/Sub topic $TOPIC"
gcloud pubsub topics describe "$TOPIC" >/dev/null 2>&1 || gcloud pubsub topics create "$TOPIC"

echo "==> Service account allowed to unlink billing (on this project only)"
gcloud iam service-accounts describe "$GUARD_SA" >/dev/null 2>&1 ||
  gcloud iam service-accounts create "$GUARD_SA_NAME" --display-name "Vaulted billing kill switch"
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member "serviceAccount:$GUARD_SA" --role roles/billing.projectManager --condition None >/dev/null
# Builds from source run as the default compute account, which newer projects create without roles.
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member "serviceAccount:$BUILD_SA" --role roles/cloudbuild.builds.builder --condition None >/dev/null

echo "==> Deploying the kill switch function"
gcloud functions deploy "$FUNCTION" --gen2 \
  --region "$REGION" --runtime python312 \
  --source deploy/billing-guard --entry-point stop_billing \
  --trigger-topic "$TOPIC" \
  --service-account "$GUARD_SA" --trigger-service-account "$GUARD_SA" \
  --set-env-vars "TARGET_PROJECT_ID=$PROJECT_ID,KILL_AT=$KILL_AT" \
  --memory 256Mi --max-instances 1 --quiet
gcloud run services add-iam-policy-binding "$FUNCTION" --region "$REGION" \
  --member "serviceAccount:$GUARD_SA" --role roles/run.invoker >/dev/null

# The function's build image also counts toward the 0.5 GB of free image storage; keep only the newest.
if gcloud artifacts repositories describe gcf-artifacts --location "$REGION" >/dev/null 2>&1; then
  gcloud artifacts repositories set-cleanup-policies gcf-artifacts --location "$REGION" \
    --policy deploy/artifact-cleanup.json --quiet >/dev/null
fi

echo "==> Budget: 1 $CURRENCY with alerts at 1% and 100%, wired to the kill switch"
if gcloud billing budgets list --billing-account "$BILLING_ACCOUNT" --format 'value(displayName)' | grep -qx "vaulted-zero"; then
  echo "    Budget 'vaulted-zero' already exists"
else
  gcloud billing budgets create --billing-account "$BILLING_ACCOUNT" \
    --display-name vaulted-zero \
    --budget-amount "1$CURRENCY" \
    --filter-projects "projects/$PROJECT_NUMBER" \
    --threshold-rule percent=0.01 --threshold-rule percent=1.0 \
    --notifications-rule-pubsub-topic "projects/$PROJECT_ID/topics/$TOPIC"
fi

echo
echo "Kill switch armed: if $PROJECT_ID's monthly cost reaches \$$KILL_AT, billing is unlinked."
echo "You'll also get budget emails at the billing account's address."
