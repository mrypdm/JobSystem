using Confluent.Kafka;
using Job.Broker.Converters;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Helpers;
using Shared.Broker.Options;

namespace Job.Broker.Clients;

/// <inheritdoc cref="IJobConsumer" />
public sealed class JobConsumer(ConsumerOptions options, ILogger<JobConsumer> logger) : IBrokerConsumer<Guid, JobMessage>
{
    private bool _disposed;

    private readonly IConsumer<Guid, JobMessage> _consumer = new ConsumerBuilder<Guid, JobMessage>(options.ToConfig())
        .SetKeyDeserializer(new GuidConverter())
        .SetValueDeserializer(new JobMessageConverter())
        .SetLogHandler(logger.GetLogHandler<IConsumer<Guid, JobMessage>>("Consumer"))
        .SetErrorHandler(logger.GetErrorHandler<IConsumer<Guid, JobMessage>>("Consumer"))
        .Build();

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false) != false)
        {
            return;
        }

        _consumer.Close();
        _consumer.Dispose();
        logger.LogInformation("Consumer closed");
    }

    /// <inheritdoc />

    public void Subscribe()
    {
        _consumer.Subscribe(options.Topic);
        logger.LogInformation("Subscribed to topic [{TopicName}]", options.Topic);
    }

    /// <inheritdoc />
    public ConsumeResult<Guid, JobMessage> Consume(CancellationToken cancellationToken)
    {
        var result = _consumer.Consume(cancellationToken);

        if (result?.Message is null)
        {
            throw new InvalidOperationException("Message is null");
        }

        if (result.Message.Key == default)
        {
            throw new InvalidOperationException("Key in message is null");
        }

        if (result.Message.Value is null)
        {
            throw new InvalidOperationException($"Value in message '{result.Message.Key}' is null");
        }

        if (result.Message.Value.Id != result.Message.Key)
        {
            throw new InvalidOperationException(
                $"Inconsistent message consumed. " +
                $"Key '{result.Message.Key}' is not equal to Value '{result.Message.Value.Id}'");
        }

        logger.LogCritical("Consumed messsage for Job [{JobId}]", result.Message.Value.Id);

        return result;
    }

    /// <inheritdoc />
    public void Commit(ConsumeResult<Guid, JobMessage> result)
    {
        _consumer.Commit(result);
        logger.LogCritical("Commited messsage for Job [{JobId}]", result.Message.Value.Id);
    }
}
