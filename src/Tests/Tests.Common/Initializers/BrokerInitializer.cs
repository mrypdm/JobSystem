using Shared.Broker.Abstractions;

namespace Tests.Common.Initializers;

/// <summary>
/// Initializer for Broker
/// </summary>
public class BrokerInitializer(IBrokerAdminClient adminClient) : IInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await adminClient.ResetAsync(cancellationToken);
        await adminClient.MigrateAsync(cancellationToken);
    }
}
