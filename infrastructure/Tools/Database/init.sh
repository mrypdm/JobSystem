#!/bin/bash

POSTGRES_DIR=~/postgres
PGADMIN_DIR=~/pgadmin

sudo rm -rf $POSTGRES_DIR/
sudo rm -rf $PGADMIN_DIR/

#
# Init postgres
#
POSTGRES_USER=70

mkdir -p $POSTGRES_DIR
mkdir -p $POSTGRES_DIR/data
mkdir -p $POSTGRES_DIR/logs
mkdir -p $POSTGRES_DIR/config

cp pg_hba.conf $POSTGRES_DIR/config/
cp ../../Certificates/certs/svc_postgres/*      $POSTGRES_DIR/config/
cp ../../Certificates/root/crl.pem              $POSTGRES_DIR/config/
echo "$1" > $POSTGRES_DIR/config/pass.txt

sudo chmod 700 $POSTGRES_DIR
sudo chmod 700 $POSTGRES_DIR/data
sudo chmod 700 $POSTGRES_DIR/logs
sudo chmod 500 $POSTGRES_DIR/config
sudo chmod 400 $POSTGRES_DIR/config/*
sudo chown $POSTGRES_USER:$POSTGRES_USER -R $POSTGRES_DIR/

if [ "$2" != "debug" ]; then
  cp docker-compose.prod.yaml docker-compose.yaml
  exit
fi

#
# Init pgadmin
#
PGADMIN_USER=5050

mkdir -p $PGADMIN_DIR
mkdir -p $PGADMIN_DIR/data
mkdir -p $PGADMIN_DIR/certs

sed "s/<PASSWORD>/$1/g" servers.json.template > $PGADMIN_DIR/servers.json
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

cp docker-compose.dev.yaml docker-compose.yaml
