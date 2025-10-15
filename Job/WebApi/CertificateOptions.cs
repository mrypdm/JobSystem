using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Job.WebApi;

/// <summary>
/// Certificate options
/// </summary>
public class CertificateOptions
{
    private readonly Lazy<X509Certificate2Collection> _chain;

    /// <summary>
    /// Creates new instance
    /// </summary>
    public CertificateOptions()
    {
        _chain = new(() => X509CertificateLoader.LoadPkcs12CollectionFromFile(CertificateFile, CertificatePassword));
    }

    /// <summary>
    /// Keystore file in PKCS12 format
    /// </summary>
    public string CertificateFile { get; set; }

    /// <summary>
    /// Password for <see cref="CertificateFile"/>
    /// </summary>
    public string CertificatePassword { get; set; }

    /// <summary>
    /// Get whole certificate chain
    /// </summary>
    public X509Certificate2Collection Chain => _chain.Value;

    /// <summary>
    /// Get certificate
    /// </summary>
    public X509Certificate2 Certificate => _chain.Value.First();
}
