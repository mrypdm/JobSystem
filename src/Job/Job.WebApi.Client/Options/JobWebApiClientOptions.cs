using Job.WebApi.Client.Clients;
using Shared.Contract.SslOptions;

namespace Job.WebApi.Client.Options;

/// <summary>
/// Options for <see cref="JobWebApiClient"/>
/// </summary>
public class JobWebApiClientOptions : Pkcs12CertificateOptions
{
    /// <summary>
    /// URL for connect
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Call timeout
    /// </summary>
    public TimeSpan Timeout { get; set; }
}
