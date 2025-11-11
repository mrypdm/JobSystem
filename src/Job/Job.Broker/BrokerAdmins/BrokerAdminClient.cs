using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Job.Broker.Migrations;
using Job.Broker.Options;
using Microsoft.Extensions.Logging;

namespace Job.Broker.BrokerAdmins;

/// <inheritdoc cref="IBrokerAdminClient"/>
public sealed class BrokerAdminClient(AdminOptions options, ILogger<BrokerAdminClient> logger)
    : IBrokerAdminClient
{
    private readonly IAdminClient _client = new AdminClientBuilder(
        new AdminClientConfig()
        {
            BootstrapServers = options.Servers,
            ClientId = options.ClientId,

            SecurityProtocol = SecurityProtocol.Ssl,
            EnableSslCertificateVerification = true,
            SslCaLocation = options.TruststoreFilePath,
            SslCertificateLocation = options.CertificateFilePath,
            SslKeyLocation = options.KeyFilePath,
            SslKeyPassword = options.Password,
            SslCrlLocation = options.RevocationListFilePath,

            Acks = Acks.Leader,
        })
        .Build();

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        logger.LogInformation("Broker admin client was disposed");
    }

    /// <inheritdoc />
    public async Task CreateTopicAsync(string topicName)
    {
        await _client.CreateTopicsAsync([new TopicSpecification()
        {
            Name = topicName,
            NumPartitions = options.PartitionsCount,
            ReplicationFactor = options.ReplicationFactor
        }]);
        logger.LogCritical("Created topic [{TopicName}] with [{PartitionsCount} | {ReplicationFactor}]",
            topicName, options.PartitionsCount, options.ReplicationFactor);
    }

    /// <inheritdoc />
    public async Task AllowTopicActionAsync(string topicName, string principal, AclOperation operation)
    {
        await _client.CreateAclsAsync([
            new AclBinding() {
                Entry = new AccessControlEntry() {
                    Principal = $"User:{principal}",
                    Host = "*",
                    Operation = operation,
                    PermissionType = AclPermissionType.Allow
                },
                Pattern = new ResourcePattern() {
                    Type = ResourceType.Topic,
                    ResourcePatternType = ResourcePatternType.Literal,
                    Name = topicName
                }
            }
        ]);
        logger.LogCritical("[{Operation}] is now allowed for [{Principal}] in [{Topic}]",
            operation, principal, topicName);
    }

    /// <inheritdoc />
    public async Task MigrateAsync()
    {
        var migrationInterface = typeof(IBrokerMigration);

        var migrationsTypes = typeof(BrokerAdminClient).Assembly.GetTypes()
            .Where(m => !m.IsInterface && !m.IsAbstract && m.IsAssignableTo(migrationInterface));

        foreach (var migrationType in migrationsTypes)
        {
            var migration = Activator.CreateInstance(migrationType) as IBrokerMigration;
            await migration.UpAsync(this);
        }
    }
}
