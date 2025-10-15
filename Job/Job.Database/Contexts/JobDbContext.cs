using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Job.Database.Contexts;

/// <summary>
/// Context for jobs
/// </summary>
public class JobDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Table of <see cref="JobDbModel"/>
    /// </summary>
    public DbSet<JobDbModel> Jobs { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Create connection string for database
    /// </summary>
    public static string GetConnectionString(DatabaseOptions databaseOptions)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = databaseOptions.HostName,
            Port = databaseOptions.Port,
            Database = databaseOptions.DatabaseName,
            SslMode = SslMode.VerifyFull,
            Username = databaseOptions.CertificateOptions.UserName,
            RootCertificate = databaseOptions.CertificateOptions.TruststoreFilePath,
            SslCertificate = databaseOptions.CertificateOptions.CertificateFilePath,
            SslKey = databaseOptions.CertificateOptions.KeyFilePath,
            SslPassword = databaseOptions.CertificateOptions.Password,
        }.ConnectionString;
    }
}
