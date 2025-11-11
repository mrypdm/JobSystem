using Confluent.Kafka.Admin;
using Shared.Broker.Abstractions;

namespace Job.Broker.Migrations;

/// <summary>
/// Initial migration for testing
/// </summary>
internal sealed class Initial : IBrokerMigration
{
    /// <inheritdoc />
    public async Task ApplyAsync(IBrokerAdminClient adminClient)
    {
        await adminClient.CreateTopicAsync("Jobs");
        await adminClient.AllowActionAsync(ResourceType.Topic, "Jobs", AclOperation.Write, "svc_jobs_webapi@kafka");
        await adminClient.AllowActionAsync(ResourceType.Topic, "Jobs", AclOperation.Read, "svc_jobs_worker@kafka");
        await adminClient.AllowActionAsync(ResourceType.Group, "Job.Worker.Group", AclOperation.Read,
            "svc_jobs_worker@kafka");
    }

    /// <inheritdoc />
    public async Task DiscardAsync(IBrokerAdminClient adminClient)
    {
        await adminClient.DisalloweActionAsync(ResourceType.Group, "Job.Worker.Group", AclOperation.Read,
            "svc_jobs_worker@kafka");
        await adminClient.DisalloweActionAsync(ResourceType.Topic, "Jobs", AclOperation.Read, "svc_jobs_worker@kafka");
        await adminClient.DisalloweActionAsync(ResourceType.Topic, "Jobs", AclOperation.Write, "svc_jobs_webapi@kafka");
        await adminClient.RemoveTopicAsync("Jobs");
    }
}
