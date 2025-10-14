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
cp ../../Certificates/svc_postgres/svc_postgres.crt postgres/config/
cp ../../Certificates/svc_postgres/svc_postgres.key postgres/config/
cp ../../Certificates/root/root.crt postgres/config/
echo "$1" > postgres/config/pass.txt

sudo chmod 700 postgres
sudo chmod 700 postgres/data
sudo chmod 700 postgres/logs
sudo chmod 500 postgres/config
sudo chmod 400 postgres/config/pg_hba.conf
sudo chmod 400 postgres/config/svc_postgres.crt
sudo chmod 400 postgres/config/svc_postgres.key
sudo chmod 400 postgres/config/root.crt
sudo chmod 400 postgres/config/pass.txt

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
cp ../../Certificates/svc_postgres/superuser/superuser.crt pgadmin/certs/
cp ../../Certificates/svc_postgres/superuser/superuser.key pgadmin/certs/
cp ../../Certificates/root/root.crt pgadmin/certs/

sudo chmod 700 pgadmin
sudo chmod 700 pgadmin/data
sudo chmod 500 pgadmin/certs
sudo chmod 600 pgadmin/servers.json
sudo chmod 400 pgadmin/certs/superuser.crt
sudo chmod 400 pgadmin/certs/superuser.key
sudo chmod 400 pgadmin/certs/root.crt

sudo chown $PGADMIN_USER:$PGADMIN_USER -R pgadmin/

cp docker-compose.dev.yaml docker-compose.yaml
