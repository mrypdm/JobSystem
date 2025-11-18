using Confluent.Kafka;

namespace Shared.Broker.Options;

/// <summary>
/// Options for <see cref="JobProducer"/>
/// </summary>
public class ProducerOptions : BrokerOptions<ProducerConfig>
{
    /// <inheritdoc />
    public override ProducerConfig ToConfig()
    {
        return new ProducerConfig()
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

            EnableDeliveryReports = true,
            DeliveryReportFields = "key",
            Acks = Acks.Leader,
        }
    }
}
