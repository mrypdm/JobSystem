namespace Shared.Broker.Abstractions;

/// <summary>
/// Migration interface for Broker
/// </summary>
public interface IBrokerMigration
{
    /// <summary>
    /// Apply migration
    /// </summary>
    Task ApplyAsync(IBrokerAdminClient adminClient);

    /// <summary>
    /// Discard migration
    /// </summary>
    Task DiscardAsync(IBrokerAdminClient adminClient);
}
