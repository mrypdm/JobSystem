#!/bin/bash

POSTGRES_DIR=~/postgres
PGADMIN_DIR=~/pgadmin

sudo rm -rf $POSTGRES_DIR/
sudo rm -rf $PGADMIN_DIR/

CERT_PASS=$(cat config/pass.txt)

#
# Init postgres
#
POSTGRES_USER=70

mkdir -p $POSTGRES_DIR
mkdir -p $POSTGRES_DIR/data
mkdir -p $POSTGRES_DIR/logs
mkdir -p $POSTGRES_DIR/config

cp config/* $POSTGRES_DIR/config/

sudo chmod 700 $POSTGRES_DIR
sudo chmod 700 $POSTGRES_DIR/data
sudo chmod 700 $POSTGRES_DIR/logs
sudo chmod 500 $POSTGRES_DIR/config
sudo chmod 400 $POSTGRES_DIR/config/*
sudo chown $POSTGRES_USER:$POSTGRES_USER -R $POSTGRES_DIR/

#
# Init pgadmin
#
PGADMIN_USER=5050

mkdir -p $PGADMIN_DIR
mkdir -p $PGADMIN_DIR/data
mkdir -p $PGADMIN_DIR/certs

sed "s/<PASSWORD>/$CERT_PASS/g" servers.json.template > $PGADMIN_DIR/servers.json
cp ../../Certificates/certs/svc_postgres/svc_postgres.truststore.pem    $PGADMIN_DIR/certs/
cp ../../Certificates/certs/superuser@postgres/*                        $PGADMIN_DIR/certs/
cp ../../Certificates/certs/svc_jobs_worker@postgres/*                  $PGADMIN_DIR/certs/
cp ../../Certificates/certs/svc_jobs_webapi@postgres/*                  $PGADMIN_DIR/certs/
cp ../../Certificates/certs/svc_users_webapp@postgres/*                  $PGADMIN_DIR/certs/

sudo chmod 700 $PGADMIN_DIR
sudo chmod 700 $PGADMIN_DIR/data
sudo chmod 500 $PGADMIN_DIR/certs
sudo chmod 600 $PGADMIN_DIR/servers.json
sudo chmod 400 $PGADMIN_DIR/certs/*

sudo chown $PGADMIN_USER:$PGADMIN_USER -R $PGADMIN_DIR/
