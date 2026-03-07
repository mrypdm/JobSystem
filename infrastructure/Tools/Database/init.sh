#!/bin/bash

POSTGRES_DIR=./data/postgres
PGADMIN_DIR=./data/pgadmin

sudo rm -rf $POSTGRES_DIR/
sudo rm -rf $PGADMIN_DIR/

CERT_PASS=$(cat ../../Certificates/root/pass.txt)

#
# Init postgres
#
POSTGRES_USER=70

mkdir -p $POSTGRES_DIR
mkdir -p $POSTGRES_DIR/data
mkdir -p $POSTGRES_DIR/logs
mkdir -p $POSTGRES_DIR/config

cp config/pg_hba.conf $POSTGRES_DIR/config/
cp ../../Certificates/certs/svc_postgres/svc_postgres.crt $POSTGRES_DIR/config/
cp ../../Certificates/certs/svc_postgres/svc_postgres.key $POSTGRES_DIR/config/
cp ../../Certificates/certs/svc_postgres/svc_postgres.truststore.pem $POSTGRES_DIR/config/
cp ../../Certificates/root/crl.pem $POSTGRES_DIR/config/
cp ../../Certificates/root/pass.txt $POSTGRES_DIR/config/

chmod 700 $POSTGRES_DIR
chmod 700 $POSTGRES_DIR/data
chmod 700 $POSTGRES_DIR/logs
chmod 500 $POSTGRES_DIR/config
chmod 400 $POSTGRES_DIR/config/*

sudo chown $POSTGRES_USER:$POSTGRES_USER -R $POSTGRES_DIR/

#
# Init pgadmin
#
PGADMIN_USER=5050

mkdir -p $PGADMIN_DIR
mkdir -p $PGADMIN_DIR/data
mkdir -p $PGADMIN_DIR/certs

sed "s/<PASSWORD>/$CERT_PASS/g" config/servers.json.template > $PGADMIN_DIR/servers.json
cp ../../Certificates/certs/svc_postgres/svc_postgres.truststore.pem    $PGADMIN_DIR/certs/
cp ../../Certificates/certs/superuser@postgres/*                        $PGADMIN_DIR/certs/
cp ../../Certificates/certs/svc_jobs_worker@postgres/*                  $PGADMIN_DIR/certs/
cp ../../Certificates/certs/svc_jobs_webapi@postgres/*                  $PGADMIN_DIR/certs/
cp ../../Certificates/certs/svc_users_webapp@postgres/*                  $PGADMIN_DIR/certs/

chmod 700 $PGADMIN_DIR
chmod 700 $PGADMIN_DIR/data
chmod 500 $PGADMIN_DIR/certs
chmod 600 $PGADMIN_DIR/servers.json
chmod 400 $PGADMIN_DIR/certs/*

sudo chown $PGADMIN_USER:$PGADMIN_USER -R $PGADMIN_DIR/
