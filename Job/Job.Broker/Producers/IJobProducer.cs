namespace Job.Broker.Producers;


/// <summary>
/// Broker producer of Jobs
/// </summary>
public interface IJobProducer
{
    /// <summary>
    /// Publish message to broker
    /// </summary>
    Task PublishAsync(JobMessage message, CancellationToken cancellationToken);
}
