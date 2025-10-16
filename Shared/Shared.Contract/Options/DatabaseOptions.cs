using Shared.Contract.SslOptions;

namespace Shared.Contract.Options;

/// <summary>
/// Options for connecting to database
/// </summary>
public class DatabaseOptions : PemCertificateOptions
{
    /// <summary>
    /// Hostname of database
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Port of database
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Name of database
    /// </summary>
    public string DatabaseName { get; set; }
}
