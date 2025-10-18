using System.Security.Cryptography.X509Certificates;

namespace Shared.Contract.SslOptions;

/// <summary>
/// Options for PKCS12 certificates
/// </summary>
public class Pkcs12CertificateOptions : CertificateOptions
{
    private readonly Lazy<X509Certificate2Collection> _certificateChain;

    /// <summary>
    /// Creates new instance
    /// </summary>
    public Pkcs12CertificateOptions()
    {
        _certificateChain = new(() => X509CertificateLoader.LoadPkcs12CollectionFromFile(CertificateFilePath, Password));
    }

    /// <summary>
    /// Get whole certificate chain
    /// </summary>
    public X509Certificate2Collection CertificateChain => _certificateChain.Value;

    /// <summary>
    /// Get certificate
    /// </summary>
    public X509Certificate2 Certificate => _certificateChain.Value.First();

    /// <summary>
    /// Get common name (username) from certificate
    /// </summary>
    public string CommonName => Certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
}
