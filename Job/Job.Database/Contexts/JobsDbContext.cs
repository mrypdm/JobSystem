using System;
using System.Threading;
using System.Threading.Tasks;
using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Contract.Models;
using Shared.Contract.Options;

namespace Job.Database.Contexts;

/// <summary>
/// Context for jobs
/// </summary>
public class JobsDbContext(DbContextOptions options, ILogger<JobsDbContext> logger) : DbContext(options)
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
            .ExecuteSqlAsync($"call p_jobs_add_new({job.Id}, {job.Timeout}, {job.Script})", cancellationToken);
        logger.LogCritical("Job [{JobId}] was added to database", job.Id);
    }

    /// <summary>
    /// Get Job for running
    /// </summary>
    public async Task<CreateJobRequest> GetNewJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<CreateJobRequest>($"select * from f_jobs_get_new({jobId})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Set Job as running
    /// </summary>
    public async Task SetJobRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"call p_jobs_set_running({jobId})", cancellationToken);
        logger.LogCritical("Job [{JobId}] set as running", jobId);
    }

    /// <summary>
    /// Set Job results
    /// </summary>
    public async Task SetJobResultsAsync(Guid jobId, JobStatus jobStatus, byte[] results, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"call p_jobs_set_results({jobId}, {jobStatus}, {results})", cancellationToken);
        logger.LogCritical("Job [{JobId}] results saved to database", jobId);
    }

    /// <summary>
    /// Get Job results
    /// </summary>
    public async Task<JobResultResponse> GetJobResults(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<JobResultResponse>($"select * from f_jobs_get_results({jobId})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobsDbContext).Assembly);
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
