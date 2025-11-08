using Job.Contract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Database;

namespace Job.Database.Contexts;

/// <inheritdoc />
public class JobDbContext(DbContextOptions options, ILogger<JobDbContext> logger)
    : PostgreDbContext(options), IJobDbContext
{
    /// <inheritdoc />
    public async Task AddNewJobAsync(CreateJobRequest job, CancellationToken cancellationToken)
    {
        try
        {
            await Database
                .ExecuteSqlAsync($"call pgdbo.p_jobs_add_new({job.Id}, {job.Timeout}, {job.Script})", cancellationToken);
            logger.LogCritical(
                "Job [{JobId}] was added to database with timeout [{JobTimeout}] and script size [{ScriptSize}]",
                job.Id, job.Timeout, job.Script.Length);
        }
        catch (PostgresException e) when (e.MessageText == "Job has been already added")
        {
            logger.LogWarning("Attempt to add duplicate job [{JobId}] to database", job.Id);
        }
    }

    /// <inheritdoc />
    public async Task<CreateJobRequest> GetNewJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<CreateJobRequest>($"select * from pgdbo.f_jobs_get_new({jobId})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetJobRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"call pgdbo.p_jobs_set_running({jobId})", cancellationToken);
        logger.LogCritical("Job [{JobId}] set as running", jobId);
    }

    /// <inheritdoc />
    public async Task SetJobResultsAsync(Guid jobId, JobStatus jobStatus, byte[] results,
        CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"call pgdbo.p_jobs_set_results({jobId}, {jobStatus}, {results})", cancellationToken);
        logger.LogCritical(
            "Job [{JobId}] results saved to database with status [{JobStatus}] and size [{ResultsSize}]",
            jobId, jobStatus, results);
    }

    /// <inheritdoc />
    public async Task<JobResultResponse> GetJobResults(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<JobResultResponse>($"select * from pgdbo.f_jobs_get_results({jobId})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkLostJobsAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var lostJobs = await Database
            .SqlQuery<Guid>($"SELECT * FROM pgdbo.f_jobs_set_lost({timeout})")
            .ToArrayAsync(cancellationToken);

        if (lostJobs.Length != 0)
        {
            logger.LogCritical("Job [{@JobIds}] marked as Lost", lostJobs);
        }
        else
        {
            logger.LogInformation("No jobs was marked as Lost");
        }
    }
}
