echo "Running for Github Actions as $(whoami) $(id)"

echo "Creating PostgreSQL environment"
mkdir -p /etc/postgres-config
cp -R /etc/github-actions/* /etc/postgres-config
chown postgres:postgres -R /etc/postgres-config
chmod 500 /etc/postgres-config
chmod 400 /etc/postgres-config/*

echo "Starting PostgreSQL"

docker-entrypoint.sh \
    -c ssl=on \
    -c ssl_min_protocol_version=TLSv1.3 \
    -c ssl_cert_file=/etc/postgres-config/svc_postgres.crt \
    -c ssl_key_file=/etc/postgres-config/svc_postgres.key \
    -c ssl_ca_file=/etc/postgres-config/svc_postgres.truststore.pem \
    -c ssl_crl_file=/etc/postgres-config/crl.pem \
    -c ssl_passphrase_command='cat /etc/postgres-config/pass.txt' \
    -c hba_file=/etc/postgres-config/pg_hba.conf
