using Confluent.Kafka;

namespace Shared.Broker.Abstractions;

/// <summary>
/// Broker consumer
/// </summary>
public interface IBrokerConsumer<TKey, TMessage> : IDisposable
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
