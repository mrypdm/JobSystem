#!/bin/bash

set -e

MTLS_ALGO="rsa:2048"
DURATION_DAYS=365
DEFAULT_PASSWORD="$1"
CERT_DIR="certs"
ROOT_DIR="root"

function get_dn() {
    echo "/C=RU/ST=Moscow/L=Moscow/O=HomeLab/OU=$1/CN=$2"
}

function init_ca() {
    rm -rf $ROOT_DIR $CERT_DIR
    mkdir -p $CERT_DIR/tmp
    mkdir -p $ROOT_DIR/crl
    touch $ROOT_DIR/index.txt
    echo 01 > $ROOT_DIR/root.srl
    echo 1000 > $ROOT_DIR/crlnumber
}

function generate_root_certificate() {
    mkdir -p $ROOT_DIR

    echo "Generating mTLS root CSR and private key"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn job_system root) \
        -keyout $ROOT_DIR/root.key \
        -out $ROOT_DIR/root.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS root certificate"
    openssl x509 \
        -req \
        -text \
        -days $DURATION_DAYS \
        -in $ROOT_DIR/root.csr \
        -extfile <(cat root.cnf) \
        -extensions v3_mtls_root \
        -key $ROOT_DIR/root.key \
        -out $ROOT_DIR/root.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm $ROOT_DIR/root.csr
}

function generate_server_certificate() {
    cn=$1
    dns=$2
    keyUsage=${3:-"serverAuth"}

    mkdir -p $CERT_DIR/$cn

    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn job_system $cn) \
        -keyout $CERT_DIR/$cn/$cn.key \
        -out $CERT_DIR/$cn.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -addext "subjectAltName = $dns" \
        -addext "extendedKeyUsage= critical, $keyUsage" \
        -quiet
    openssl ca -config root.cnf -notext -batch -in $CERT_DIR/$cn.csr -out $CERT_DIR/$cn/$cn.crt -extensions v3_mtls_server -passin "pass:$DEFAULT_PASSWORD"
    rm $CERT_DIR/$cn.csr
}

function generate_client_certificate() {
    cn=$1

    mkdir -p $CERT_DIR/$cn

    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn job_system $cn) \
        -keyout $CERT_DIR/$cn/$cn.key \
        -out $CERT_DIR/$cn.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet
    openssl ca -config root.cnf -notext -batch -in $CERT_DIR/$cn.csr -out $CERT_DIR/$cn/$cn.crt -extensions v3_mtls_client -passin "pass:$DEFAULT_PASSWORD"

    rm $CERT_DIR/$cn.csr
}

function create_pem_truststore() {
    cn=$1
    mkdir -p $CERT_DIR/$cn
    echo "Creating PEM truststore for $cn"
    cp $ROOT_DIR/root.crt "$CERT_DIR/$cn/$cn.truststore.pem"
}

function create_pkcs12_truststore() {
    cn=$1
    mkdir -p $CERT_DIR/$cn
    echo "Creating PKCS12 truststore for $cn"
    keytool \
        -keystore $CERT_DIR/$cn/$cn.truststore.p12 \
        -alias CA-root \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file $ROOT_DIR/root.crt \
        -noprompt
}

function create_pkcs12_keystore() {
    cn=$1
    mkdir -p $CERT_DIR/$cn

    echo "Creating PKCS12 keystore for $cn"
    openssl pkcs12 \
        -export \
        -in $CERT_DIR/$cn/$cn.crt \
        -inkey $CERT_DIR/$cn/$cn.key \
        -out $CERT_DIR/$cn/$cn.p12.tmp \
        -name $cn \
        -passin "pass:$DEFAULT_PASSWORD" \
        -passout "pass:$DEFAULT_PASSWORD"

    keytool \
        -keystore $CERT_DIR/$cn/$cn.keystore.p12 \
        -alias CA-root \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file $ROOT_DIR/root.crt \
        -noprompt
    keytool \
        -importkeystore \
        -deststorepass "$DEFAULT_PASSWORD" \
        -destkeypass "$DEFAULT_PASSWORD" \
        -destkeystore $CERT_DIR/$cn/$cn.keystore.p12 \
        -srckeystore $CERT_DIR/$cn/$cn.p12.tmp \
        -srcstoretype PKCS12 \
        -srcstorepass "$DEFAULT_PASSWORD" \
        -alias $cn

    rm $CERT_DIR/$cn/$cn.p12.tmp
}

init_ca

# Root
generate_root_certificate

# PostgreSQL
create_pem_truststore "svc_postgres"
generate_server_certificate "svc_postgres" "IP:127.0.0.1,DNS:localhost,DNS:postgres"
generate_client_certificate "svc_jobs_webapi@postgres"
generate_client_certificate "svc_jobs_worker@postgres"
generate_client_certificate "svc_users_webapp@postgres"
generate_client_certificate "superuser@postgres"
create_pkcs12_keystore "svc_jobs_webapi@postgres"
create_pkcs12_keystore "svc_jobs_worker@postgres"
create_pkcs12_keystore "svc_users_webapp@postgres"
create_pkcs12_keystore "superuser@postgres"

# Kafka
create_pem_truststore "svc_kafka"
create_pkcs12_truststore "svc_kafka"
generate_server_certificate "svc_kafka" "IP:127.0.0.1,DNS:localhost,DNS:kafka" "serverAuth, clientAuth"
generate_client_certificate "svc_jobs_webapi@kafka"
generate_client_certificate "svc_jobs_worker@kafka"
generate_client_certificate "superuser@kafka"
create_pkcs12_keystore "svc_kafka"
create_pkcs12_keystore "superuser@kafka"
create_pkcs12_keystore "svc_jobs_webapi@kafka"
create_pkcs12_keystore "svc_jobs_worker@kafka"

# Jobs.WebApi
generate_server_certificate "svc_jobs_webapi" "IP:127.0.0.1,DNS:localhost,DNS:jobs-webapi"
generate_client_certificate "svc_users_webapp@webapi"
generate_client_certificate "superuser@webapi"
create_pkcs12_keystore "svc_jobs_webapi"
create_pkcs12_keystore "superuser@webapi"
create_pkcs12_keystore "svc_users_webapp@webapi"

# Users.WebApp
generate_server_certificate "svc_users_webapp" "IP:127.0.0.1,DNS:localhost,DNS:users-webapp"
create_pkcs12_keystore "svc_users_webapp"

# Test certs
generate_server_certificate "svc_testhost@revoked" "IP:127.0.0.1,DNS:testhost"
create_pkcs12_keystore "svc_testhost@revoked"
generate_server_certificate "svc_testhost" "IP:127.0.0.1,DNS:testhost"
create_pkcs12_keystore "svc_testhost"
openssl ca -config root.cnf -revoke certs/svc_testhost@revoked/svc_testhost@revoked.crt -passin "pass:$DEFAULT_PASSWORD"

rm -rf $CERT_DIR/tmp

openssl ca -config root.cnf -gencrl -crldays $DURATION_DAYS -out root/crl.pem -passin "pass:$DEFAULT_PASSWORD"
