using Confluent.Kafka;

namespace Job.Broker.Consumers;

/// <summary>
/// Broker consumer of Jobs
/// </summary>
public interface IJobConsumer
{
    /// <summary>
    /// Subceribe to Broker
    /// </summary>
    void Subscribe();

    /// <summary>
    /// Consume message
    /// </summary>
    ConsumeResult<Guid, JobMessage> Consume(CancellationToken cancellationToken);

    /// <summary>
    /// Commit consumed message
    /// </summary>
    void Commit(ConsumeResult<Guid, JobMessage> result);

}
