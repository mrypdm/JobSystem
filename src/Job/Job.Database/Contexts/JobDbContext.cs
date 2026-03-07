using Job.Contract;
using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Shared.Contract.Extensions;
using Shared.Database;

namespace Job.Database.Contexts;

/// <inheritdoc />
public class JobDbContext(DbContextOptions options, ILogger logger)
    : PostgreDbContext(options, logger), IJobDbContext
{
    /// <summary>
    /// Jobs table
    /// </summary>
    internal DbSet<JobDbModel> Jobs { get; set; }

    /// <inheritdoc />
    public async Task AddNewJobAsync(NewJobModel job, CancellationToken cancellationToken)
    {
        try
        {
            await Database
                .ExecuteSqlAsync($"CALL pgdbo.p_jobs_add_new({job.Id}, {job.Timeout}, {job.Script})", cancellationToken);
            Logger.Critical().Information(
                "Job [{JobId}] was added to database with timeout [{JobTimeout}] and script size [{ScriptSize}]",
                job.Id, job.Timeout, job.Script.Length);
        }
        catch (PostgresException e) when (e.MessageText == "Job has been already added")
        {
            Logger.Warning("Attempt to add duplicate job [{JobId}] to database", job.Id);
        }
    }

    /// <inheritdoc />
    public async Task<NewJobModel> GetNewJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<NewJobModel>($"SELECT * FROM pgdbo.f_jobs_get_new({jobId})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetJobRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"CALL pgdbo.p_jobs_set_running({jobId})", cancellationToken);
        Logger.Critical().Information("Job [{JobId}] set as running", jobId);
    }

    /// <inheritdoc />
    public async Task SetJobResultsAsync(Guid jobId, JobStatus jobStatus, byte[] results,
        CancellationToken cancellationToken)
    {
        await Database
            .ExecuteSqlAsync($"CALL pgdbo.p_jobs_set_results({jobId}, {jobStatus}, {results})", cancellationToken);
        Logger.Critical().Information(
            "Job [{JobId}] results saved to database with status [{JobStatus}] and size [{ResultsSize}]",
            jobId, jobStatus, results.Length);
    }

    /// <inheritdoc />
    public async Task<JobResultResponse> GetJobResultsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<JobResultResponse>($"SELECT * FROM pgdbo.f_jobs_get_results({jobId})")
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
            Logger.Critical().Information("Job [{@JobIds}] marked as Lost", lostJobs);
        }
        else
        {
            Logger.Information("No jobs was marked as Lost");
        }
    }
}
