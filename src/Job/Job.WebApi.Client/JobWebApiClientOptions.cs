using Shared.Contract.SslOptions;

namespace Job.WebApi.Client;

/// <summary>
/// Options for <see cref="JobWebApiClient"/>
/// </summary>
public class JobWebApiClientOptions : Pkcs12CertificateOptions
{
    /// <summary>
    /// URL for connect
    /// </summary>
    public string Url { get; set; }
}
