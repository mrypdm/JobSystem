using Job.Broker.BrokerAdmins;

namespace Job.Broker.Migrations;

/// <summary>
/// Migration interface for Broker
/// </summary>
internal interface IBrokerMigration
{
    /// <summary>
    /// Apply migration
    /// </summary>
    Task UpAsync(IBrokerAdminClient adminClient);
}
