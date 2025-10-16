using System;
using System.Threading;
using System.Threading.Tasks;
using Job.Contract;
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

    /// <summary>
    /// Add new Job to database
    /// </summary>
    public async Task AddNewJobAsync(CreateJobRequest job, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"exec p_jobs_addnew {job.Id} {job.Timeout} {job.Steps}", cancellationToken);
    }

    /// <summary>
    /// Get Job for running
    /// </summary>
    public async Task<CreateJobRequest> GetNewJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<CreateJobRequest>($"exec p_jobs_getnew {jobId}")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Set Job as running
    /// </summary>
    public async Task SetJobRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"exec p_jobs_setrunning {jobId}", cancellationToken);
    }

    /// <summary>
    /// Set Job results
    /// </summary>
    public async Task SetJobResultsAsync(Guid jobId, JobStatus jobStatus, byte[] results, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"exec p_jobs_setresults {jobId} {jobStatus} {results}", cancellationToken);
    }

    /// <summary>
    /// Get Job results
    /// </summary>
    public async Task<JobResultResponse> GetJobResults(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<JobResultResponse>($"exec p_jobs_getresults {jobId}")
            .SingleOrDefaultAsync(cancellationToken);
    }

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
