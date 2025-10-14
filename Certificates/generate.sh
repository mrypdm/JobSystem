MTLS_ALGO="rsa:8192"

DURATION_DAYS=365

DEFAULT_C="RU"
DEFAULT_ST="Moscow"
DEFAULT_L="Moscow"
DEFAULT_O="mrypdm"
DEFAULT_OU="localhost"
DEFAULT_EMAIL="mrypdm@gmail.com"

DEFAULT_PASSWORD="$1"

MTLS_DN_S="C=${DEFAULT_C}, ST=${DEFAULT_ST}, L=${DEFAULT_L}, O=${DEFAULT_O}, OU=${DEFAULT_OU}, emailAddress=${DEFAULT_EMAIL}, CN="
MTLS_DN="/${MTLS_DN_S//, //}"

function generate_root_certificate() {
    mkdir -p root

    echo "Generating mTLS root CSR and private key"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj "$MTLS_DN"CARoot \
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
        -extfile <(cat /etc/ssl/openssl.cnf openssl.cnf) \
        -extensions v3_mtls_root \
        -key root/root.key \
        -out root/root.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    echo "Creating mTLS root truststore"
    rm root/root.p12
    keytool \
        -keystore root/root.p12 \
        -alias caroot \
        -storepass "$DEFAULT_PASSWORD" \
        -importcert -file root/root.crt \
        -noprompt

    rm root/root.csr
}

function generate_server_certificate() {
    server_name=$1

    mkdir -p $server_name

    echo "Generating mTLS $server_name server CSR and private key"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj "$MTLS_DN"$server_name \
        -keyout $server_name/$server_name.key \
        -out $server_name/$server_name.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS $server_name server certificate using previously generated root certificate"
    openssl x509 \
        -req \
        -text \
        -CAcreateserial \
        -days $DURATION_DAYS \
        -in $server_name/$server_name.csr \
        -extfile <(cat /etc/ssl/openssl.cnf openssl.cnf) \
        -extensions v3_mtls_$server_name \
        -CA root/root.crt \
        -CAkey root/root.key \
        -out $server_name/$server_name.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm $server_name/$server_name.csr
}

function generate_client_certificate() {
    server_name=$1
    client_name=$2

    mkdir -p $server_name/$client_name

    echo "Generating mTLS $server_name client CSR and private key for $client_name"
    openssl req \
        -new \
        -text \
        -newkey $MTLS_ALGO \
        -subj "$MTLS_DN"$client_name \
        -keyout $server_name/$client_name/$client_name.key \
        -out $server_name/$client_name/$client_name.csr \
        -passout "pass:$DEFAULT_PASSWORD" \
        -quiet

    echo "Generating mTLS $server_name client certificate for $client_name using previously generated root certificate"
    openssl x509 \
        -req \
        -text \
        -CAcreateserial \
        -days $DURATION_DAYS \
        -in $server_name/$client_name/$client_name.csr \
        -extfile <(cat /etc/ssl/openssl.cnf openssl.cnf) \
        -extensions v3_mtls_client \
        -CA root/root.crt \
        -CAkey root/root.key \
        -out $server_name/$client_name/$client_name.crt \
        -passin "pass:$DEFAULT_PASSWORD"

    rm $server_name/$client_name/$client_name.csr
}

function pem_to_pkcs12() {
    cert_path=$1
    cert_name=$2

    echo "Creating PKCS12 keystore for $cert_path/$cert_name"
    openssl pkcs12 \
        -export \
        -in $cert_path/$cert_name.crt \
        -inkey $cert_path/$cert_name.key \
        -out $cert_path/$cert_name.p12 \
        -name $cert_name \
        -certfile root/root.crt \
        -CAfile root/root.crt \
        -caname caroot \
        -passin "pass:$DEFAULT_PASSWORD" \
        -passout "pass:$DEFAULT_PASSWORD"

    keytool -keystore $cert_path/$cert_name.p12 -alias caroot -storepass "$DEFAULT_PASSWORD" -importcert -file root/root.crt -noprompt
}


generate_root_certificate

generate_server_certificate svc_postgres
generate_client_certificate svc_postgres superuser
#generate_client_certificate svc_postgres svc_jobs_webapi
#generate_client_certificate svc_postgres svc_jobs_worker
#generate_client_certificate svc_postgres svc_users_webapp


generate_server_certificate svc_kafka
generate_client_certificate svc_kafka superuser
#generate_client_certificate svc_kafka svc_jobs_webapi
#generate_client_certificate svc_kafka svc_jobs_worker

pem_to_pkcs12 svc_kafka svc_kafka
pem_to_pkcs12 svc_kafka/superuser superuser
#pem_to_pkcs12 svc_kafka/svc_jobs_webapi svc_jobs_webapi
#pem_to_pkcs12 svc_kafka/svc_jobs_worker svc_jobs_worker
