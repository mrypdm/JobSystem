echo "Running for Github Actions as $(whoami) $(id)"

echo "Creating Apache Kafka environment"
mkdir -p /etc/kafka/secrets
cp -R /etc/github-actions/* /etc/kafka/secrets
chown postgres:postgres -R /etc/kafka/secrets
chmod 500 /etc/kafka/secrets
chmod 400 /etc/kafka/secrets*

echo "Starting Apache Kafka"

/__cacert_entrypoint.sh /etc/kafka/docker/run
