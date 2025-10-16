#!/bin/bash

set -e

MTLS_ALGO="rsa:2048"

DURATION_DAYS=365

DEFAULT_C="RU"
DEFAULT_ST="Moscow"
DEFAULT_L="Moscow"
DEFAULT_O="mrypdm"

DEFAULT_PASSWORD="$1"

function get_dn() {
    echo "/C=${DEFAULT_C}/ST=${DEFAULT_ST}/L=${DEFAULT_L}/O=${DEFAULT_O}/OU=$1/CN=$2"
}

function generate_root_certificate() {
    mkdir -p root

    echo "Generating mTLS root CSR and private key"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn CA root) \
        -keyout root/root.key \
        -out root/root.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS root certificate"
    openssl x509 \
        -req \
        -text \
        -days $DURATION_DAYS \
        -in root/root.csr \
        -extfile <(cat /etc/ssl/openssl.cnf common.cnf root.cnf) \
        -extensions v3_mtls_root \
        -key root/root.key \
        -out root/root.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm root/root.csr
}

function generate_intermediate_certificate() {
    ou_name=$1

    mkdir -p $ou_name

    echo "Generating mTLS $ou_name intermediate CSR and private key"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn CA $ou_name) \
        -keyout $ou_name/$ou_name.key \
        -out $ou_name/$ou_name.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS $ou_name intermediate certificate using previously generated root certificate"
    openssl x509 \
        -req \
        -text \
        -CAcreateserial \
        -days $DURATION_DAYS \
        -in $ou_name/$ou_name.csr \
        -extfile <(cat /etc/ssl/openssl.cnf common.cnf root.cnf) \
        -extensions v3_mtls_intermediate \
        -CA root/root.crt \
        -CAkey root/root.key \
        -out $ou_name/$ou_name.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm $ou_name/$ou_name.csr
}

function generate_server_certificate() {
    ou_name=$1
    server_name=$2

    mkdir -p $ou_name/$server_name

    echo "Generating mTLS $server_name server CSR and private key"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn $ou_name $server_name) \
        -keyout $ou_name/$server_name/$server_name.key \
        -out $ou_name/$server_name/$server_name.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS $server_name server certificate using previously generated root certificate"
    openssl x509 \
        -req \
        -text \
        -CAcreateserial \
        -days $DURATION_DAYS \
        -in $ou_name/$server_name/$server_name.csr \
        -extfile <(cat /etc/ssl/openssl.cnf common.cnf servers.cnf) \
        -extensions v3_mtls_$server_name \
        -copy_extensions copy \
        -CA $ou_name/$ou_name.crt \
        -CAkey $ou_name/$ou_name.key \
        -out $ou_name/$server_name/$server_name.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm $ou_name/$server_name/$server_name.csr
}

function generate_client_certificate() {
    ou_name=$1
    client_name=$2

    mkdir -p $ou_name/$client_name

    echo "Generating mTLS $client_name client CSR and private key for $ou_name"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj $(get_dn $ou_name $client_name) \
        -keyout $ou_name/$client_name/$client_name.key \
        -out $ou_name/$client_name/$client_name.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS $client_name client certificate for $ou_name using previously generated root certificate"
    openssl x509 \
        -req \
        -text \
        -CAcreateserial \
        -days $DURATION_DAYS \
        -in $ou_name/$client_name/$client_name.csr \
        -extfile <(cat /etc/ssl/openssl.cnf common.cnf clients.cnf) \
        -extensions v3_mtls_client \
        -CA $ou_name/$ou_name.crt \
        -CAkey $ou_name/$ou_name.key \
        -out $ou_name/$client_name/$client_name.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm $ou_name/$client_name/$client_name.csr
}

function create_pkcs12_truststore() {
    ou_name=$1

    echo "Creating PKCS12 truststore for $ou_name"

    keytool \
        -keystore $ou_name/$ou_name.p12 \
        -alias CA-root \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file root/root.crt \
        -noprompt
    keytool \
        -keystore $ou_name/$ou_name.p12 \
        -alias CA-$ou_name-intermediate \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file $ou_name/$ou_name.crt \
        -noprompt
}

function create_pem_truststore() {
    ou_name=$1

    echo "Creating PEM truststore for $ou_name"

    cat root/root.crt "$ou_name/$ou_name.crt" > "$ou_name/$ou_name.chain.crt"
}

function pem_to_pkcs12() {
    ou_name=$1
    cert_name=$2

    echo "Creating PKCS12 keystore for $ou_name/$cert_name"
    openssl pkcs12 \
        -export \
        -in $ou_name/$cert_name/$cert_name.crt \
        -inkey $ou_name/$cert_name/$cert_name.key \
        -out $ou_name/$cert_name/$cert_name.p12.private \
        -name $cert_name \
        -passin "pass:$DEFAULT_PASSWORD" \
        -passout "pass:$DEFAULT_PASSWORD"

    keytool \
        -keystore $ou_name/$cert_name/$cert_name.p12 \
        -alias CA-root \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file root/root.crt \
        -noprompt
    keytool \
        -keystore $ou_name/$cert_name/$cert_name.p12 \
        -alias CA-$ou_name-intermediate \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file $ou_name/$ou_name.crt \
        -noprompt
    keytool \
        -importkeystore \
        -deststorepass "$DEFAULT_PASSWORD" \
        -destkeypass "$DEFAULT_PASSWORD" \
        -destkeystore $ou_name/$cert_name/$cert_name.p12 \
        -srckeystore $ou_name/$cert_name/$cert_name.p12.private \
        -srcstoretype PKCS12 \
        -srcstorepass "$DEFAULT_PASSWORD" \
        -alias $cert_name

    rm $ou_name/$cert_name/$cert_name.p12.private
}

rm -rf root postgres kafka webapi webapp

generate_root_certificate

# PostgreSQL
generate_intermediate_certificate postgres
create_pem_truststore postgres
generate_server_certificate postgres svc_postgres
generate_client_certificate postgres superuser
generate_client_certificate postgres svc_jobs_webapi
generate_client_certificate postgres svc_jobs_worker
generate_client_certificate postgres svc_users_webapp

# Kafka
generate_intermediate_certificate kafka
create_pkcs12_truststore kafka
create_pem_truststore kafka
generate_server_certificate kafka svc_kafka
generate_client_certificate kafka superuser
generate_client_certificate kafka svc_jobs_webapi
generate_client_certificate kafka svc_jobs_worker
pem_to_pkcs12 kafka svc_kafka
pem_to_pkcs12 kafka superuser
pem_to_pkcs12 kafka svc_jobs_webapi
pem_to_pkcs12 kafka svc_jobs_worker

# Jobs.WebApi
generate_intermediate_certificate webapi
generate_server_certificate webapi svc_jobs_webapi
pem_to_pkcs12 webapi svc_jobs_webapi
generate_client_certificate webapi superuser
pem_to_pkcs12 webapi superuser
generate_client_certificate webapi svc_users_webapp
pem_to_pkcs12 webapi svc_users_webapp

# User.WebApp
generate_intermediate_certificate webapp
generate_server_certificate webapp svc_users_webapp
pem_to_pkcs12 webapp svc_users_webapp
