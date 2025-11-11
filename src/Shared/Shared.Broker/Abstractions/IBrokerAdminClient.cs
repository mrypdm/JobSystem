using Confluent.Kafka.Admin;

namespace Shared.Broker.Abstractions;

/// <summary>
/// Admin client for Broker
/// </summary>
public interface IBrokerAdminClient : IDisposable
{
    /// <summary>
    /// Create new Topic with name <paramref name="topic"/>
    /// </summary>
    Task CreateTopicAsync(string topic);

    /// <summary>
    /// Remove Topic with name <paramref name="topicName"/>
    /// </summary>
    Task RemoveTopicAsync(string topicName);

    /// <summary>
    /// Allow <paramref name="principal"/> to do <paramref name="operation"/> in resource
    /// with name<paramref name="resourceName"/> of type <paramref name="resourceType"/>
    /// </summary>
    Task AllowActionAsync(ResourceType resourceType, string resourceName, AclOperation operation,
        string principal);

    /// <summary>
    /// Disallow permission for <paramref name="principal"/> to do <paramref name="operation"/> in resource
    /// with name<paramref name="resourceName"/> of type <paramref name="resourceType"/>
    /// </summary>
    Task DisalloweActionAsync(ResourceType resourceType, string resourceName, AclOperation operation,
        string principal);

    /// <summary>
    /// Migrate Broker
    /// </summary>
    Task MigrateAsync();

    /// <summary>
    /// Reset Broker
    /// </summary>
    Task ResetAsync();
}
