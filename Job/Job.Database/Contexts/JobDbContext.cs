using Job.Contract;
using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Database;

namespace Job.Database.Contexts;

/// <summary>
/// Context for jobs
/// </summary>
public class JobDbContext(DbContextOptions options, ILogger<JobDbContext> logger) : PostgreDbContext(options)
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
    /// Mark Jobs with timeout vialoation as lost
    /// </summary>
    public async Task MarkLostJobsAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var lostJobs = await Database
            .SqlQuery<Guid>($"SELECT * FROM f_jobs_set_lost({timeout})")
            .ToArrayAsync(cancellationToken);
        logger.LogCritical("Job [{@JobIds}] marked as Lost", lostJobs);
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
