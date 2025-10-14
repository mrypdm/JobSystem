using System.Security.Cryptography.X509Certificates;

namespace Job.WebApi;

/// <summary>
/// Certificate options
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// Truststore file in PKCS12 format
    /// </summary>
    public string RootFile { get; set; }

    /// <summary>
    /// Password for <see cref="RootFile"/>
    /// </summary>
    public string RootPassword { get; set; }

    /// <summary>
    /// Keystore file in PKCS12 format
    /// </summary>
    public string CertificateFile { get; set; }

    /// <summary>
    /// Password for <see cref="CertificateFile"/>
    /// </summary>
    public string CertificatePassword { get; set; }

    /// <summary>
    /// Get certificate
    /// </summary>
    public X509Certificate2 GetCertificate()
    {
        return X509CertificateLoader.LoadPkcs12FromFile(CertificateFile, CertificatePassword);
    }

    /// <summary>
    /// Get truststore
    /// </summary>
    public X509Certificate2Collection GetTrusted()
    {
        return X509CertificateLoader.LoadPkcs12CollectionFromFile(RootFile, RootPassword);
    }

    /// <summary>
    /// Get whole certificate chain
    /// </summary>
    public X509Certificate2Collection GetChain()
    {
        return [GetCertificate(), .. GetTrusted()];
    }
}
