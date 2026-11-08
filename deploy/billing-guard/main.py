"""Billing kill switch for the Vaulted Google Cloud project.

Cloud Billing budgets publish the month's cost to Pub/Sub several times a day. When that cost
reaches KILL_AT (default $0.01), this function unlinks the project from its billing account:
every paid service stops, so charges can't keep growing. Re-link billing in the console to undo.
"""
import base64
import json
import os

import functions_framework
from google.cloud import billing_v1

PROJECT_ID = os.environ["TARGET_PROJECT_ID"]
KILL_AT = float(os.environ.get("KILL_AT", "0.01"))


@functions_framework.cloud_event
def stop_billing(cloud_event):
    message = json.loads(base64.b64decode(cloud_event.data["message"]["data"]))
    cost = float(message.get("costAmount", 0))
    if cost < KILL_AT:
        print(f"Cost {cost:.4f} is under {KILL_AT}; nothing to do.")
        return

    client = billing_v1.CloudBillingClient()
    name = f"projects/{PROJECT_ID}"
    if not client.get_project_billing_info(name=name).billing_enabled:
        print("Billing is already disabled.")
        return

    client.update_project_billing_info(
        name=name,
        project_billing_info=billing_v1.ProjectBillingInfo(billing_account_name=""),
    )
    print(f"Billing DISABLED for {PROJECT_ID}: month cost {cost:.4f} reached {KILL_AT}.")
