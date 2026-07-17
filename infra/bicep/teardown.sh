#!/usr/bin/env bash
# Deletes the entire resource group — the whole point of the "minimal footprint"
# approach is that everything Bicep created can be torn down in one shot between
# demos and recreated later with deploy.sh.
#
# NOT RUN in this session — no az CLI / Azure credentials in that sandbox.
#
# Usage: RESOURCE_GROUP=commerce-app-lab-rg ./teardown.sh

set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:?Set RESOURCE_GROUP}"

echo "This will permanently delete resource group '$RESOURCE_GROUP' and everything in it."
read -r -p "Type the resource group name to confirm: " CONFIRM
if [ "$CONFIRM" != "$RESOURCE_GROUP" ]; then
  echo "Confirmation did not match — aborting."
  exit 1
fi

az group delete --name "$RESOURCE_GROUP" --yes --no-wait
echo "Deletion started (--no-wait). Check progress with: az group show --name $RESOURCE_GROUP"
