using Confluent.Kafka.Admin;
using Shared.Broker.Abstractions;

namespace Job.Broker.Migrations;

/// <summary>
/// Initial migration for testing
/// </summary>
internal sealed class Initial : IBrokerMigration
{
    /// <inheritdoc />
    public async Task UpAsync(IBrokerAdminClient adminClient)
    {
        await adminClient.CreateTopicAsync("Jobs");
        await adminClient.AllowTopicActionAsync("Jobs", "svc_jobs_webapi@kafka", AclOperation.Write);
        await adminClient.AllowTopicActionAsync("Jobs", "svc_jobs_worker@kafka", AclOperation.Read);
    }
}
