using Shared.Contract.SslOptions;

namespace Shared.Database;

/// <summary>
/// Options for connecting to database
/// </summary>
public class DatabaseOptions : Pkcs12CertificateOptions
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

    /// <summary>
    /// Count of retries on failure
    /// </summary>
    public int RetriesCount { get; set; } = 10;

    /// <summary>
    /// Delay between retries
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);

}
