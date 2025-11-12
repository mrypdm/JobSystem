using Shared.Broker.Abstractions;

namespace Tests.Unit.Initializers;

/// <summary>
/// Initializer for Broker
/// </summary>
internal class BrokerInitializer(IBrokerAdminClient adminClient) : IInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await adminClient.ResetAsync();
        await adminClient.MigrateAsync();
    }
}
