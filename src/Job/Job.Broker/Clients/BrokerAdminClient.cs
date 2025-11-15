using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;

namespace Job.Broker.Clients;

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

            Acks = Acks.All,
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
    public async Task RemoveTopicAsync(string topicName)
    {
        await _client.DeleteTopicsAsync([topicName]);
        logger.LogCritical("Topic [{TopicName}] was deleted", topicName);
    }

    /// <inheritdoc />
    public async Task AllowActionAsync(ResourceType resourceType, string resourceName, AclOperation operation,
        string principal)
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
                    Type = resourceType,
                    ResourcePatternType = ResourcePatternType.Literal,
                    Name = resourceName
                }
            }
        ]);
        logger.LogCritical("[{Operation}] is now allowed for [{Principal}] in [{ResourceType} {ResourceName}]",
            operation, principal, resourceType, resourceName);
    }

    /// <inheritdoc />
    public async Task DisalloweActionAsync(ResourceType resourceType, string resourceName, AclOperation operation,
        string principal)
    {
        await _client.DeleteAclsAsync([
            new AclBindingFilter(){
                EntryFilter = new AccessControlEntryFilter() {
                    Principal = $"User:{principal}",
                    Host = "*",
                    Operation = operation,
                    PermissionType = AclPermissionType.Allow
                },
                PatternFilter = new ResourcePatternFilter() {
                    Type = resourceType,
                    ResourcePatternType = ResourcePatternType.Literal,
                    Name = resourceName
                }
            }
        ]);
        logger.LogCritical("[{Operation}] is now disallowed for [{Principal}] in [{ResourceType} {ResourceName}]",
            operation, principal, resourceType, resourceName);
    }

    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        var migrationInterface = typeof(IBrokerMigration);

        var migrationsTypes = GetType().Assembly.GetTypes()
            .Where(m => !m.IsInterface && !m.IsAbstract && m.IsAssignableTo(migrationInterface));

        foreach (var migrationType in migrationsTypes)
        {
            var migration = Activator.CreateInstance(migrationType) as IBrokerMigration;
            await migration.ApplyAsync(this, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var migrationInterface = typeof(IBrokerMigration);

        var migrationsTypes = GetType().Assembly.GetTypes()
            .Where(m => !m.IsInterface && !m.IsAbstract && m.IsAssignableTo(migrationInterface));

        foreach (var migrationType in migrationsTypes)
        {
            var migration = Activator.CreateInstance(migrationType) as IBrokerMigration;
            await migration.DiscardAsync(this, cancellationToken);
        }
    }
}
