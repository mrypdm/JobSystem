using Shared.Contract.SslOptions;

namespace Shared.Contract.Options;

/// <summary>
/// Options for mTLS authentication in web server
/// </summary>
public class WebServerOptions : Pkcs12CertificateOptions
{
    /// <summary>
    /// Is application working behind proxy
    /// </summary>
    public bool IsProxyUsed { get; set; }
}
