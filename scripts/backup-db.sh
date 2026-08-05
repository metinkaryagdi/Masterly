#!/usr/bin/env bash
# Dumps the Postgres database running in the "db" compose service to a
# timestamped, gzip-compressed file under ./backups.
#
# Usage: ./scripts/backup-db.sh [backups-dir]
# Schedule it with cron, e.g. daily at 03:00:
#   0 3 * * * cd /path/to/Training_App && ./scripts/backup-db.sh >> backups/backup.log 2>&1

set -euo pipefail

BACKUP_DIR="${1:-backups}"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
OUT_FILE="${BACKUP_DIR}/training_platform_${TIMESTAMP}.sql.gz"

mkdir -p "$BACKUP_DIR"

docker compose exec -T db pg_dump -U postgres --format=plain training_platform | gzip > "$OUT_FILE"

echo "Backup written to ${OUT_FILE}"
