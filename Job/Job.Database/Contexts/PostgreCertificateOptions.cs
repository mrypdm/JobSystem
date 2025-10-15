namespace Job.Database.Contexts;

/// <summary>
/// Options for mTLS authentication for PostgreSQL
/// </summary>
public class PostgreCertificateOptions
{
    /// <summary>
    /// Path to truststore in PEM format
    /// </summary>
    public string TruststoreFilePath { get; set; }

    /// <summary>
    /// Username of client
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Path to client certificate in PEM format
    /// </summary>
    public string CertificateFilePath { get; set; }

    /// <summary>
    /// Path to client private key in PEM format
    /// </summary>
    public string KeyFilePath { get; set; }

    /// <summary>
    /// Password for private key
    /// </summary>
    public string Password { get; set; }
}
