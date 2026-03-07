using Confluent.Kafka;
using Job.Broker.Converters;
using Serilog;
using Shared.Broker.Abstractions;
using Shared.Broker.Helpers;
using Shared.Broker.Options;
using Shared.Contract.Extensions;

namespace Job.Broker.Clients;

/// <inheritdoc cref="IJobProducer"/>
public sealed class JobProducer : IBrokerProducer<Guid, JobMessage>
{
    private bool _disposed;

    private readonly IProducer<Guid, JobMessage> _producer;
    private readonly ProducerOptions _options;
    private readonly ILogger _logger;

    public JobProducer(ProducerOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger.ForContext<JobProducer>();
        _producer = new ProducerBuilder<Guid, JobMessage>(options.ToConfig())
            .SetKeySerializer(new GuidConverter())
            .SetValueSerializer(new JobMessageConverter())
            .SetLogHandler(_logger.GetLogHandler<IProducer<Guid, JobMessage>>("Producer"))
            .SetErrorHandler(_logger.GetErrorHandler<IProducer<Guid, JobMessage>>("Producer"))
            .Build();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false) != false)
        {
            return;
        }

        _producer.Flush();
        _producer.Dispose();
        _logger.Information("Producer closed");
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
            await _producer.ProduceAsync(_options.Topic, brokerMessage, cancellationToken).ConfigureAwait(false);
            _logger.Critical().Information("Message for Job [{JobId}] published", message.Id);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Cannot publish message for Job [{JobId}]", message.Id);
            throw;
        }
    }
}
