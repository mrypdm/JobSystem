#!/bin/bash

sudo rm -rf kafka/
sudo rm -rf kafka-ui/

#
# Init Kafka
#
KAFKA_USER=1000

mkdir -p kafka/data
mkdir -p kafka/secrets

cp ../../Certificates/svc_kafka/svc_kafka.p12 kafka/secrets/
cp ../../Certificates/root/root.p12 kafka/secrets/
echo "$1" > kafka/secrets/pass.txt

sudo chmod 700 kafka
sudo chmod 700 kafka/data
sudo chmod 500 kafka/secrets
sudo chmod 400 kafka/secrets/svc_kafka.p12
sudo chmod 400 kafka/secrets/root.p12
sudo chmod 400 kafka/secrets/pass.txt

sudo chown $KAFKA_USER:$KAFKA_USER -R kafka/

if [ "$2" != "debug" ]; then
  cp docker-compose.prod.yaml docker-compose.yaml
  exit
fi

#
# Init Kafka UI
#
KAFKA_UI_USER=6060

mkdir -p kafka-ui
mkdir -p kafka-ui/secrets

cp ../../Certificates/svc_kafka/superuser/superuser.p12 kafka-ui/secrets/
cp ../../Certificates/root/root.p12 kafka-ui/secrets/
sed "s/<PASSWORD>/$1/g" config.yaml.template > kafka-ui/config.yaml

sudo chmod 700 kafka-ui
sudo chmod 500 kafka-ui/secrets
sudo chmod 600 kafka-ui/config.yaml
sudo chmod 400 kafka-ui/secrets/superuser.p12
sudo chmod 400 kafka-ui/secrets/root.p12

sudo chown $KAFKA_UI_USER:$KAFKA_UI_USER -R kafka-ui/

cp docker-compose.dev.yaml docker-compose.yaml
