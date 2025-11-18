using Confluent.Kafka;

namespace Shared.Broker.Options;

/// <summary>
/// Admin options for Broker
/// </summary>
public class AdminOptions : BrokerOptions<AdminClientConfig>
{
    /// <summary>
    /// Count of partitions for new Topics
    /// </summary>
    public int PartitionsCount { get; set; }

    /// <summary>
    /// Replication factor for new Topics
    /// </summary>
    public short ReplicationFactor { get; set; }

    /// <inheritdoc />
    public override AdminClientConfig ToConfig()
    {
        return new AdminClientConfig()
        {
            BootstrapServers = Servers,
            ClientId = ClientId,

            SecurityProtocol = SecurityProtocol.Ssl,
            EnableSslCertificateVerification = true,
            SslCaLocation = TruststoreFilePath,
            SslCertificateLocation = CertificateFilePath,
            SslKeyLocation = KeyFilePath,
            SslKeyPassword = Password,
            SslCrlLocation = RevocationListFilePath,

            Acks = Acks.All,
        }
    }
}
