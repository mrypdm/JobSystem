using Confluent.Kafka;
using Job.Broker.Options;
using Microsoft.Extensions.Logging;

namespace Job.Broker;

/// <summary>
/// Broker producer of Jobs
/// </summary>
public sealed class JobProducer(ProducerOptions options, ILogger<JobProducer> logger) : IDisposable
{
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
            Acks = Acks.Leader
        })
        .Build();

    /// <inheritdoc />
    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
        logger.LogInformation("Producer closed");
    }

    /// <summary>
    /// Publish message to broker
    /// </summary>
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
        }
    }
}
