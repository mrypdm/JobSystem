using Confluent.Kafka;

namespace Shared.Broker.Options;

/// <summary>
/// Options for <see cref="JobConsumer"/>
/// </summary>
public class ConsumerOptions : BrokerOptions<ConsumerConfig>
{
    /// <summary>
    /// Group Id
    /// </summary>
    public string GroupId { get; set; }

    /// <inheritdoc />
    public override ConsumerConfig ToConfig()
    {
        return new ConsumerConfig
        {
            BootstrapServers = Servers,
            ClientId = ClientId,
            GroupId = GroupId,

            SecurityProtocol = SecurityProtocol.Ssl,
            EnableSslCertificateVerification = true,
            SslCaLocation = TruststoreFilePath,
            SslCertificateLocation = CertificateFilePath,
            SslKeyLocation = KeyFilePath,
            SslKeyPassword = Password,
            SslCrlLocation = RevocationListFilePath,

            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            Acks = Acks.Leader
        };
    }
}
