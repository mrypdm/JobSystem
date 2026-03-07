using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using Shared.Broker.Abstractions;
using Shared.Broker.Helpers;
using Shared.Broker.Options;
using Shared.Contract.Extensions;

namespace Job.Broker.Clients;

/// <inheritdoc cref="IBrokerAdminClient"/>
public sealed class BrokerAdminClient : IBrokerAdminClient
{
    private bool _disposed;
    private readonly ILogger _logger;
    private readonly AdminOptions _options;
    private readonly IAdminClient _client;

    public BrokerAdminClient(AdminOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger.ForContext<BrokerAdminClient>();
        _client = new AdminClientBuilder(options.ToConfig())
            .SetLogHandler(_logger.GetLogHandler<IAdminClient>("AdminClient"))
            .SetErrorHandler(_logger.GetErrorHandler<IAdminClient>("AdminClient"))
            .Build();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false) != false)
        {
            return;
        }

        _client.Dispose();
        _logger.Information("Broker admin client was disposed");
    }

    /// <inheritdoc />
    public async Task CreateTopicAsync(string topicName)
    {
        await _client.CreateTopicsAsync([new TopicSpecification()
        {
            Name = topicName,
            NumPartitions = _options.PartitionsCount,
            ReplicationFactor = _options.ReplicationFactor
        }]);
        _logger.Critical().Warning("Created topic [{TopicName}] with [{PartitionsCount} | {ReplicationFactor}]",
            topicName, _options.PartitionsCount, _options.ReplicationFactor);
    }

    /// <inheritdoc />
    public async Task RemoveTopicAsync(string topicName)
    {
        await _client.DeleteTopicsAsync([topicName]);
        _logger.Critical().Warning("Topic [{TopicName}] was deleted", topicName);
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
        _logger.Critical().Warning("[{Operation}] is now allowed for [{Principal}] in [{ResourceType} {ResourceName}]",
            operation, principal, resourceType, resourceName);
    }

    /// <inheritdoc />
    public async Task DisallowActionAsync(ResourceType resourceType, string resourceName, AclOperation operation,
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
        _logger.Critical().Warning("[{Operation}] is now disallowed for [{Principal}] in [{ResourceType} {ResourceName}]",
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
            _logger.Critical().Warning("Migration [{MigrationName}] was applied", migration.GetType().Name);
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
            _logger.Critical().Warning("Migration [{MigrationName}] was discarded", migration.GetType().Name);
        }
    }
}
