#!/usr/bin/env bash
# Migrates Products (and optionally Carts, Orders) from Azure SQL into Cosmos DB.
# Usage: SQL_SERVER=<name> SQL_PASSWORD=<pw> COSMOS_ACCOUNT=<name> ./scripts/migrate-to-cosmos.sh [Products Carts Orders]
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-rgDeveloperEscapeRoom-02}"
SQL_SERVER="${SQL_SERVER:?SQL_SERVER is required}"
SQL_DB="${SQL_DB:-StyleVerseDb}"
SQL_USER="${SQL_USER:-styleverseadmin}"
SQL_PASSWORD="${SQL_PASSWORD:?SQL_PASSWORD is required}"
COSMOS_ACCOUNT="${COSMOS_ACCOUNT:?COSMOS_ACCOUNT is required}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/cosmos"
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

CONTAINERS=("$@")
[[ ${#CONTAINERS[@]} -eq 0 ]] && CONTAINERS=(Products)

python3 -m venv "$WORK_DIR/venv"
"$WORK_DIR/venv/bin/pip" install --quiet azure-cosmos

export COSMOS_ENDPOINT
COSMOS_ENDPOINT="$(az cosmosdb show -g "$RESOURCE_GROUP" -n "$COSMOS_ACCOUNT" --query documentEndpoint -o tsv)"
export COSMOS_KEY
COSMOS_KEY="$(az cosmosdb keys list -g "$RESOURCE_GROUP" -n "$COSMOS_ACCOUNT" --query primaryMasterKey -o tsv)"

for container in "${CONTAINERS[@]}"; do
  query_file="$SCRIPT_DIR/$(echo "$container" | tr '[:upper:]' '[:lower:]').sql"
  out_file="$WORK_DIR/$container.jsonl"

  SQLCMDPASSWORD="$SQL_PASSWORD" sqlcmd \
    -S "$SQL_SERVER.database.windows.net" -d "$SQL_DB" -U "$SQL_USER" \
    -h -1 -W -y 0 -b -i "$query_file" -o "$out_file"

  "$WORK_DIR/venv/bin/python" "$SCRIPT_DIR/upload.py" "$container" "$out_file"
done
