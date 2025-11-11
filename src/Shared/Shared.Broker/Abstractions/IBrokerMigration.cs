namespace Shared.Broker.Abstractions;

/// <summary>
/// Migration interface for Broker
/// </summary>
public interface IBrokerMigration
{
    /// <summary>
    /// Apply migration
    /// </summary>
    Task UpAsync(IBrokerAdminClient adminClient);
}
