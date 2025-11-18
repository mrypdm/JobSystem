namespace Shared.Broker.Abstractions;

/// <summary>
/// Broker producer
/// </summary>
public interface IBrokerProducer<TKey, TMessage> : IDisposable
{
    /// <summary>
    /// Publish message to broker
    /// </summary>
    Task PublishAsync(TMessage message, CancellationToken cancellationToken);
}
