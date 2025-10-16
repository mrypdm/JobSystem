using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contract;

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
            Username = databaseOptions.UserName,
            RootCertificate = databaseOptions.TruststoreFilePath,
            SslCertificate = databaseOptions.CertificateFilePath,
            SslKey = databaseOptions.KeyFilePath,
            SslPassword = databaseOptions.Password,
        }.ConnectionString;
    }
}
