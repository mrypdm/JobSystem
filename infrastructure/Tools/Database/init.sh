#!/bin/bash

sudo rm -rf postgres/
sudo rm -rf pgadmin/

#
# Init postgres
#
POSTGRES_USER=70

mkdir -p postgres
mkdir -p postgres/data
mkdir -p postgres/logs
mkdir -p postgres/config

cp pg_hba.conf postgres/config/
cp ../../Certificates/certs/svc_postgres/*      postgres/config/
cp ../../Certificates/root/crl.pem              postgres/config/
echo "$1" > postgres/config/pass.txt

sudo chmod 700 postgres
sudo chmod 700 postgres/data
sudo chmod 700 postgres/logs
sudo chmod 500 postgres/config
sudo chmod 400 postgres/config/*
sudo chown $POSTGRES_USER:$POSTGRES_USER -R postgres/

if [ "$2" != "debug" ]; then
  cp docker-compose.prod.yaml docker-compose.yaml
  exit
fi

#
# Init pgadmin
#
PGADMIN_USER=5050

mkdir -p pgadmin
mkdir -p pgadmin/data
mkdir -p pgadmin/certs

sed "s/<PASSWORD>/$1/g" servers.json.template > pgadmin/servers.json
cp ../../Certificates/certs/svc_postgres/svc_postgres.truststore.pem    pgadmin/certs/
cp ../../Certificates/certs/superuser@postgres/*                        pgadmin/certs/
cp ../../Certificates/certs/svc_jobs_worker@postgres/*                  pgadmin/certs/
cp ../../Certificates/certs/svc_jobs_webapi@postgres/*                  pgadmin/certs/
cp ../../Certificates/certs/svc_users_webapp@postgres/*                  pgadmin/certs/

sudo chmod 700 pgadmin
sudo chmod 700 pgadmin/data
sudo chmod 500 pgadmin/certs
sudo chmod 600 pgadmin/servers.json
sudo chmod 400 pgadmin/certs/*

sudo chown $PGADMIN_USER:$PGADMIN_USER -R pgadmin/

cp docker-compose.dev.yaml docker-compose.yaml
