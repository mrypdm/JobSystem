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

        // Sometimes Kafka doesn't have time to load the ACL, causing the test to fail with authorization error
        // So please wait 3 seconds for the settings to be applied.
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
    }
}
