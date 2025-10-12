MTLS_ALGO="rsa:2048"

DURATION_DAYS=365

DEFAULT_C="RU"
DEFAULT_ST="Moscow"
DEFAULT_L="Moscow"
DEFAULT_O="mrypdm"
DEFAULT_OU="localhost"
DEFAULT_EMAIL="mrypdm@gmail.com"

MTLS_DN_S="C=${DEFAULT_C}, ST=${DEFAULT_ST}, L=${DEFAULT_L}, O=${DEFAULT_O}, OU=${DEFAULT_OU}, emailAddress=${DEFAULT_EMAIL}, CN="
MTLS_DN="/${MTLS_DN_S//, //}"

function generate_root_certificate() {
    umask u=rwx,go= && mkdir -p root

    echo "Generating mTLS root CSR and private key"
    umask u=rw,go= && \
        openssl req \
            -new \
            -text \
            -newkey $MTLS_ALGO \
            -subj "$MTLS_DN""mrypdm root ca" \
            -keyout root/root.key \
            -out root/root.csr

    echo "Generating mTLS root certificate"
    umask u=rw,go= && \
        openssl x509 \
            -req \
            -text \
            -days $DURATION_DAYS \
            -in root/root.csr \
            -extfile <(cat /etc/ssl/openssl.cnf openssl.cnf) \
            -extensions v3_mtls_root \
            -key root/root.key \
            -out root/root.crt

    rm root/root.csr
}

function generate_server_certificate() {
    server_name=$1

    umask u=rwx,go= && mkdir -p $server_name

    echo "Generating mTLS $server_name server CSR and private key"
    umask u=rw,go= && \
        openssl req \
            -new \
            -text \
            -newkey $MTLS_ALGO \
            -subj "$MTLS_DN"$server_name \
            -keyout $server_name/$server_name.key \
            -out $server_name/$server_name.csr

    echo "Generating mTLS $server_name server certificate using previously generated root certificate"
    umask u=rw,go= && \
        openssl x509 \
            -req \
            -text \
            -CAcreateserial \
            -days $DURATION_DAYS \
            -in $server_name/$server_name.csr \
            -extfile <(cat /etc/ssl/openssl.cnf openssl.cnf) \
            -extensions v3_mtls_server_$server_name \
            -CA root/root.crt \
            -CAkey root/root.key \
            -out $server_name/$server_name.crt

    rm $server_name/$server_name.csr
}

function generate_client_certificate() {
    server_name=$1
    client_name=$2

    umask u=rwx,go= && mkdir -p $server_name/$client_name

    echo "Generating mTLS $server_name client CSR and private key for $client_name"
        umask u=rw,go= && \
            openssl req \
                -new \
                -text \
                -newkey $MTLS_ALGO \
                -subj "$MTLS_DN"$client_name \
                -keyout $server_name/$client_name/$client_name.key \
                -out $server_name/$client_name/$client_name.csr

    echo "Generating mTLS $server_name client certificate for $client_name using previously generated root certificate"
    umask u=rw,go= && \
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
            -out $server_name/$client_name/$client_name.crt

    rm $server_name/$client_name/$client_name.csr

}

generate_root_certificate

generate_server_certificate postgres
generate_client_certificate postgres superuser
generate_client_certificate postgres svc_jobs_webapi
generate_client_certificate postgres svc_jobs_worker
generate_client_certificate postgres svc_users_webapp


generate_server_certificate zookeeper
generate_server_certificate kafka
generate_client_certificate kafka superuser
generate_client_certificate kafka svc_jobs_webapi
generate_client_certificate kafka svc_jobs_worker
