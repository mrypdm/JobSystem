using Shared.Contract.SslOptions;

namespace User.WebApp.Models;

/// <summary>
/// Options for connect to Job.WebApi
/// </summary>
public class JobWebApiOptions : Pkcs12CertificateOptions
{
    /// <summary>
    /// URL for connect
    /// </summary>
    public string Url { get; set; }
}
