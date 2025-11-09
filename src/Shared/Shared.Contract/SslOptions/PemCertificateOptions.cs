using System.Security.Cryptography.X509Certificates;

namespace Shared.Contract.SslOptions;

public class PemCertificateOptions : CertificateOptions
{
    private string _username;

    /// <summary>
    /// Username of client
    /// </summary>
    public string UserName => _username ??= X509Certificate2
        .CreateFromEncryptedPemFile(CertificateFilePath, Password, KeyFilePath)
        .GetNameInfo(X509NameType.SimpleName, forIssuer: false);

    /// <summary>
    /// Path to truststore
    /// </summary>
    public string TruststoreFilePath { get; set; }

    /// <summary>
    /// Path to client private key
    /// </summary>
    public string KeyFilePath { get; set; }
}
