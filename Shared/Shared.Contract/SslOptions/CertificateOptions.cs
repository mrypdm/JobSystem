namespace Shared.Contract.SslOptions;

/// <summary>
/// Options for mTLS authentication
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// Path to revocation list
    /// </summary>
    public string RevocationListFilePath { get; set; }

    /// <summary>
    /// Path to client certificate
    /// </summary>
    public string CertificateFilePath { get; set; }

    /// <summary>
    /// Password for private key
    /// </summary>
    public string Password { get; set; }
}
