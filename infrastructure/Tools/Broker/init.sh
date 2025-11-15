#!/bin/bash

KAFKA_DIR=~/kafka
KAFKA_UI_DIR=~/kafka-ui

sudo rm -rf $KAFKA_DIR/
sudo rm -rf $KAFKA_UI_DIR/

CERT_PASS=$(cat config/pass.txt)

#
# Init Kafka
#
KAFKA_USER=1000

mkdir -p $KAFKA_DIR
mkdir -p $KAFKA_DIR/data
mkdir -p $KAFKA_DIR/secrets

cp config/* $KAFKA_DIR/secrets/

sudo chmod 700 $KAFKA_DIR
sudo chmod 700 $KAFKA_DIR/data
sudo chmod 500 $KAFKA_DIR/secrets
sudo chmod 400 $KAFKA_DIR/secrets/*

sudo chown $KAFKA_USER:$KAFKA_USER -R $KAFKA_DIR/

if [ "$1" != "debug" ]; then
  cp docker-compose.prod.yaml docker-compose.yaml
  exit
fi

#
# Init Kafka UI
#
KAFKA_UI_USER=6060

mkdir -p $KAFKA_UI_DIR
mkdir -p $KAFKA_UI_DIR/secrets

cp ../../Certificates/certs/superuser@kafka/superuser@kafka.keystore.p12    $KAFKA_UI_DIR/secrets/
cp ../../Certificates/certs/svc_kafka/svc_kafka.truststore.p12              $KAFKA_UI_DIR/secrets/
sed "s/<PASSWORD>/$CERT_PASS/g" config.yaml.template > $KAFKA_UI_DIR/config.yaml

sudo chmod 700 $KAFKA_UI_DIR
sudo chmod 500 $KAFKA_UI_DIR/secrets
sudo chmod 600 $KAFKA_UI_DIR/config.yaml
sudo chmod 400 $KAFKA_UI_DIR/secrets/*

sudo chown $KAFKA_UI_USER:$KAFKA_UI_USER -R $KAFKA_UI_DIR/

cp docker-compose.dev.yaml docker-compose.yaml
