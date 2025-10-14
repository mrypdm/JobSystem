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

exit

# создание truststore
keytool -keystore kafka.client.trustore.jks -alias CARoot -storepass mrypdm56m-43 -importcert -file ../root/root.crt -noprompt

keytool -keystore kafka.server.trustore.jks -alias CARoot -storepass mrypdm56m-43 -importcert -file ../root/root.crt -noprompt

# создание из CRT и KEY (PEM) одного PKCS12 файла
openssl pkcs12 -export -in superuser.crt -inkey superuser.key -out superuser.p12 -name superuser -CAfile ../../root/root.crt -caname root

# превращение PKCS12 в Java Keystore
keytool -importkeystore -deststorepass mrypdm56m-43 -destkeypass mrypdm56m-43 -destkeystore superuser.keystore -srckeystore superuser.p12 -srcstoretype PKCS12 -srcstorepass mrypdm56m-43 -alias superuser

# https://stackoverflow.com/questions/75489955/kafka-producer-in-net-ssl-handshake-failed вроде и не надо jks
# https://zookeeper.apache.org/doc/r3.7.1/zookeeperAdmin.html : ssl.trustStore.type
