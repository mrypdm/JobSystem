using Shared.Broker.Abstractions;

namespace Tests.Integration.Initializers;

/// <summary>
/// Initializer for Broker
/// </summary>
internal class BrokerInitializer(IBrokerAdminClient adminClient) : BaseInitializer
{
    /// <inheritdoc />
    protected override Task InitializeInternalAsync(CancellationToken cancellationToken)
    {
        return adminClient.MigrateAsync();
    }
}
