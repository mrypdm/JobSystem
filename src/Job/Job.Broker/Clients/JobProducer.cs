using Confluent.Kafka;
using Job.Broker.Converters;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Helpers;
using Shared.Broker.Options;

namespace Job.Broker.Clients;

/// <inheritdoc cref="IJobProducer"/>
public sealed class JobProducer(ProducerOptions options, ILogger<JobProducer> logger) : IBrokerProducer<Guid, JobMessage>
{
    private bool _disposed;

    private readonly IProducer<Guid, JobMessage> _producer = new ProducerBuilder<Guid, JobMessage>(options.ToConfig())
        .SetKeySerializer(new GuidConverter())
        .SetValueSerializer(new JobMessageConverter())
        .SetLogHandler(logger.GetLogHandler<IProducer<Guid, JobMessage>>("Producer"))
        .SetErrorHandler(logger.GetErrorHandler<IProducer<Guid, JobMessage>>("Producer"))
        .Build();

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false) != false)
        {
            return;
        }

        _producer.Flush();
        _producer.Dispose();
        logger.LogInformation("Producer closed");
    }

    /// <inheritdoc />
    public async Task PublishAsync(JobMessage message, CancellationToken cancellationToken)
    {
        var brokerMessage = new Message<Guid, JobMessage>()
        {
            Key = message.Id,
            Value = message,
            Timestamp = Timestamp.Default
        };

        try
        {
            await _producer.ProduceAsync(options.Topic, brokerMessage, cancellationToken).ConfigureAwait(false);
            logger.LogCritical("Message for Job [{JobId}] published", message.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot publish message for Job [{JobId}]", message.Id);
            throw;
        }
    }
}
