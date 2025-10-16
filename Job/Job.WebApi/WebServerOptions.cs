using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Shared.Contract;

namespace Job.WebApi;

/// <summary>
/// Options for mTLS authentication in web server
/// </summary>
public class WebServerOptions : CertificateOptions
{
    private readonly Lazy<X509Certificate2Collection> _chain;

    /// <summary>
    /// Creates new instance
    /// </summary>
    public WebServerOptions()
    {
        _chain = new(() => X509CertificateLoader.LoadPkcs12CollectionFromFile(CertificateFilePath, Password));
    }

    /// <summary>
    /// Get whole certificate chain
    /// </summary>
    public X509Certificate2Collection Chain => _chain.Value;

    /// <summary>
    /// Get certificate
    /// </summary>
    public X509Certificate2 Certificate => _chain.Value.First();
}
