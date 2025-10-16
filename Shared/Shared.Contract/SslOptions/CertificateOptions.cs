namespace Shared.Contract.SslOptions;

/// <summary>
/// Options for mTLS authentication
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// Path to client certificate
    /// </summary>
    public string CertificateFilePath { get; set; }

    /// <summary>
    /// Password for private key
    /// </summary>
    public string Password { get; set; }
}
