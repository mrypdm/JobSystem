using System;
using System.Threading;
using Confluent.Kafka;
using Job.Broker.Options;
using Microsoft.Extensions.Logging;

namespace Job.Broker;

/// <summary>
/// Broker consumer of Jobs
/// </summary>
public sealed class JobConsumer : IDisposable
{
    private readonly ConsumerOptions _options;
    private readonly ILogger<JobConsumer> _logger;
    private readonly IConsumer<Guid, JobMessage> _consumer;

    public JobConsumer(ConsumerOptions options, ILogger<JobConsumer> logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Servers,
            ClientId = options.ClientId,
            GroupId = options.GroupId,

            SecurityProtocol = SecurityProtocol.Ssl,
            EnableSslCertificateVerification = true,
            SslCaLocation = options.TruststoreFilePath,
            SslCertificateLocation = options.CertificateFilePath,
            SslKeyLocation = options.KeyFilePath,
            SslKeyPassword = options.Password,

            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            Acks = Acks.Leader
        };

        _options = options;
        _logger = logger;
        _consumer = new ConsumerBuilder<Guid, JobMessage>(config).Build();
    }

    /// <summary>
    /// Subceribe to Broker
    /// </summary>
    public void Subscribe()
    {
        _consumer.Subscribe(_options.Topic);
        _logger.LogInformation("Subscribed to topic [{TopicName}]", _options.Topic);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        _logger.LogInformation("Consumer closed");
    }

    /// <summary>
    /// Consume message
    /// </summary>
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

        _logger.LogCritical("Consumed messsage for Job [{JobId}]", result.Message.Value.Id);

        return result;
    }

    /// <summary>
    /// Commit consumed message
    /// </summary>
    public void Commit(ConsumeResult<Guid, JobMessage> result)
    {
        _consumer.Commit(result);
        _logger.LogCritical("Commited messsage for Job [{JobId}]", result.Message.Value.Id);
    }
}
