namespace Shared.Contract.SslOptions;

public class PemCertificateOptions : CertificateOptions
{
    /// <summary>
    /// Username of client
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Path to truststore
    /// </summary>
    public string TruststoreFilePath { get; set; }

    /// <summary>
    /// Path to client private key
    /// </summary>
    public string KeyFilePath { get; set; }
}
