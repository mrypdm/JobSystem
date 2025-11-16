using Confluent.Kafka;
using Job.Broker.Converters;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;

namespace Job.Broker.Clients;

/// <inheritdoc cref="IJobProducer"/>
public sealed class JobProducer(ProducerOptions options, ILogger<JobProducer> logger) : IJobProducer<Guid, JobMessage>
{
    private bool _disposed;

    private readonly IProducer<Guid, JobMessage> _producer = new ProducerBuilder<Guid, JobMessage>(
        new ProducerConfig()
        {
            BootstrapServers = options.Servers,
            ClientId = options.ClientId,

            SecurityProtocol = SecurityProtocol.Ssl,
            EnableSslCertificateVerification = true,
            SslCaLocation = options.TruststoreFilePath,
            SslCertificateLocation = options.CertificateFilePath,
            SslKeyLocation = options.KeyFilePath,
            SslKeyPassword = options.Password,
            SslCrlLocation = options.RevocationListFilePath,

            EnableDeliveryReports = true,
            DeliveryReportFields = "key",
            Acks = Acks.Leader,
        })
        .SetKeySerializer(new GuidConverter())
        .SetValueSerializer(new JobMessageConverter())
        .SetLogHandler((_, logMessage) =>
        {
            logger.Log(
                (LogLevel)logMessage.LevelAs(LogLevelType.MicrosoftExtensionsLogging),
                "[{ProducerName}] {Message}", logMessage.Name, logMessage.Message);
        })
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
