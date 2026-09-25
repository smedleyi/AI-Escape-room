#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-rgDeveloperEscapeRoom-02}"
SERVER_NAME="${SERVER_NAME:-}"
DB_NAME="${DB_NAME:-StyleVerseDb}"
ADMIN_USER="${ADMIN_USER:-styleverseadmin}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-}"
SQL_FILE="${SQL_FILE:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/database.sql}"

if [[ -z "${SERVER_NAME}" ]]; then
  echo "SERVER_NAME is required. Example: SERVER_NAME=sql-styleverse-abc123 ./scripts/seed-sql.sh" >&2
  exit 1
fi

if [[ -z "${ADMIN_PASSWORD}" ]]; then
  echo "ADMIN_PASSWORD is required." >&2
  exit 1
fi

if [[ ! -f "${SQL_FILE}" ]]; then
  echo "SQL file not found: ${SQL_FILE}" >&2
  exit 1
fi

sqlcmd \
  -S "${SERVER_NAME}.database.windows.net" \
  -d "${DB_NAME}" \
  -U "${ADMIN_USER}" \
  -P "${ADMIN_PASSWORD}" \
  -i "${SQL_FILE}" \
  -C \
  -b

printf '\nSQL seeding completed successfully for %s/%s\n' "${SERVER_NAME}" "${DB_NAME}"
