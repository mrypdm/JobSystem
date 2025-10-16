using System;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly IMessageHandler _handler;
    private readonly IConsumer<Guid, JobMessage> _consumer;

    public JobConsumer(ConsumerOptions options, ILogger<JobConsumer> logger, IMessageHandler handler)
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
        _handler = handler;
        _consumer = new ConsumerBuilder<Guid, JobMessage>(config).Build();
    }

    /// <summary>
    /// Start consuming
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.Subscribe(_options.Topic);
        return ConsumeLoopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = ConsumeSingle(cancellationToken);
                await _handler.HandleAsync(result.Message.Value, cancellationToken);
                _consumer.Commit(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot consume message");
            }

            try
            {
                await Task.Delay(_options.IterationDeplay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // NOP
            }
        }
    }

    private ConsumeResult<Guid, JobMessage> ConsumeSingle(CancellationToken cancellationToken)
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

        return result;
    }
}
