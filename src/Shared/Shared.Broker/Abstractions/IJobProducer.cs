namespace Shared.Broker.Abstractions;

/// <summary>
/// Broker producer of Jobs
/// </summary>
public interface IJobProducer<TKey, TMessage> : IDisposable
{
    /// <summary>
    /// Publish message to broker
    /// </summary>
    Task PublishAsync(TMessage message, CancellationToken cancellationToken);
}
