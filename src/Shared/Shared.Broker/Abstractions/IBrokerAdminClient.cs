using Confluent.Kafka.Admin;

namespace Shared.Broker.Abstractions;

/// <summary>
/// Admin client for Broker
/// </summary>
public interface IBrokerAdminClient : IDisposable
{
    /// <summary>
    /// Allow <paramref name="principal"/> to do <paramref name="operation"/> in <paramref name="topic"/>
    /// </summary>
    Task AllowTopicActionAsync(string topic, string principal, AclOperation operation);

    /// <summary>
    /// Create new Topic with name <paramref name="topic"/>
    /// </summary>
    Task CreateTopicAsync(string topic);

    /// <summary>
    /// Migrate Broker
    /// </summary>
    Task MigrateAsync();
}
