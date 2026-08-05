#!/usr/bin/env bash
# Restores a backup produced by backup-db.sh into the running "db" compose
# service. DESTRUCTIVE: drops and recreates the target database first.
#
# Usage: ./scripts/restore-db.sh backups/training_platform_20260803_030000.sql.gz

set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <backup-file.sql.gz>" >&2
  exit 1
fi

BACKUP_FILE="$1"
if [[ ! -f "$BACKUP_FILE" ]]; then
  echo "Backup file not found: $BACKUP_FILE" >&2
  exit 1
fi

read -r -p "This will DROP and recreate 'codecraftnet_db' on the running db service. Continue? [y/N] " CONFIRM
if [[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]]; then
  echo "Aborted."
  exit 1
fi

docker compose exec -T db psql -U postgres -c "DROP DATABASE IF EXISTS codecraftnet_db;"
docker compose exec -T db psql -U postgres -c "CREATE DATABASE codecraftnet_db;"
gunzip -c "$BACKUP_FILE" | docker compose exec -T db psql -U postgres -d codecraftnet_db

echo "Restore complete from ${BACKUP_FILE}"
