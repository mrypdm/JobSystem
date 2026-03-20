#!/bin/bash

MODE="$1"

BASE_DIR=/home/mrypdm/Projects/JobSystem/tmp
TIMESTAMP=$(date +%s)

function full() {
    SAVE_TO=${BASE_DIR}/pg_base_${TIMESTAMP}/

    echo "Saving full backup to ${SAVE_TO}"

    pg_basebackup --host=localhost --user=superuser@postgres --format=tar \
        --pgdata=${SAVE_TO}
}

function incremental() {
    LATEST_BACKUP=$(ls -Art ${BASE_DIR} | tail -n 1)
    if [ -z ${LATEST_BACKUP} ]; then
        echo "Cannot find base backup for incremental backup"
        exit 2
    fi

    SAVE_TO=${BASE_DIR}/pg_inc_${TIMESTAMP}/
    BASED_ON=${BASE_DIR}/${LATEST_BACKUP}

    echo "Saving incremental backup to ${SAVE_TO} based on ${LATEST_BACKUP}"

    pg_basebackup --host=localhost --user=superuser@postgres --format=tar \
        --pgdata=${SAVE_TO} \
        --incremental=${LATEST_BACKUP}/backup_manifest
}

if [ "$MODE" == "FULL" ]; then
    full
elif [ "$MODE" == "INC" ]; then
    incremental
else
    echo "First argument must be 'FULL' for full backup or 'INC' for incremental"
    exit 1
fi

# For auto-backup cron can be used, e.g.
# Full backup on each first day of each month
#    0 10 1 * * docker exec postgres /etc/postgres-config/backup.sh FULL
# Incremental backup on each day, except first day, of each month
#    0 10 2-31 * * docker exec postgres /etc/postgres-config/backup.sh INC
