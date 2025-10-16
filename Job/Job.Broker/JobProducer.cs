using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Job.Broker.Options;
using Microsoft.Extensions.Logging;

namespace Job.Broker;

/// <summary>
/// Broker producer of Jobs
/// </summary>
public sealed class JobProducer : IDisposable
{
    private readonly ProducerOptions _options;
    private readonly ILogger<JobProducer> _logger;
    private readonly IProducer<Guid, JobMessage> _producer;

    /// <summary>
    /// Creates new instance
    /// </summary>
    public JobProducer(ProducerOptions options, ILogger<JobProducer> logger)
    {
        var config = new ProducerConfig()
        {
            BootstrapServers = options.Servers,
            ClientId = options.ClientId,

            SecurityProtocol = SecurityProtocol.Ssl,
            EnableSslCertificateVerification = true,
            SslCaLocation = options.TruststoreFilePath,
            SslCertificateLocation = options.CertificateFilePath,
            SslKeyLocation = options.KeyFilePath,
            SslKeyPassword = options.Password,

            EnableDeliveryReports = true,
            DeliveryReportFields = "key",
            Acks = Acks.Leader
        };

        _options = options;
        _logger = logger;
        _producer = new ProducerBuilder<Guid, JobMessage>(config).Build();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
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
            await _producer.ProduceAsync(_options.Topic, brokerMessage, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Message for Job [{JobId}] published", message.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Cannot publish message for Job [{JobId}]", message.Id);
        }
    }
}
