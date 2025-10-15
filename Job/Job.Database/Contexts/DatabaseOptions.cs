namespace Job.Database.Contexts;

/// <summary>
/// Options for connecting to database
/// </summary>
public class DatabaseOptions
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
    /// Certificate options of client and database for secure and authenticated connection
    /// </summary>
    public PostgreCertificateOptions CertificateOptions { get; set; }
}
