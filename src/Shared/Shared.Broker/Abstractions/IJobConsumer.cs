using Confluent.Kafka;

namespace Shared.Broker.Abstractions;

/// <summary>
/// Broker consumer of Jobs
/// </summary>
public interface IJobConsumer<TKey, TMessage> : IDisposable
{
    /// <summary>
    /// Subceribe to Broker
    /// </summary>
    void Subscribe();

    /// <summary>
    /// Consume message
    /// </summary>
    ConsumeResult<TKey, TMessage> Consume(CancellationToken cancellationToken);

    /// <summary>
    /// Commit consumed message
    /// </summary>
    void Commit(ConsumeResult<TKey, TMessage> result);

}
