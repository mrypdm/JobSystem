using Job.Contract;
using Job.Worker.Models;
using Job.Worker.Processes;
using Microsoft.Extensions.Logging;

namespace Job.Worker.JobProcesses;

/// <summary>
/// Run Job process in Docker
/// </summary>
public class DockerJobProcessRunner(IProcessRunner processRunner, ILogger<DockerJobProcessRunner> logger)
    : IJobProcessRunner
{
    /// <inheritdoc />
    public async Task RunProcessAsync(RunJobModel jobModel)
    {
        ArgumentNullException.ThrowIfNull(jobModel);
        if (string.IsNullOrWhiteSpace(jobModel.Directory))
        {
            throw new InvalidOperationException(
                $"Cannot run process for Job '{jobModel.Id}' because its environment is not initialized");
        }

        using var jobTimeoutCancellation = new CancellationTokenSource(jobModel.Timeout);
        logger.LogCritical("Starting process for Job [{JobId}] with timeout [{Timeout}]",
            jobModel.Id, jobModel.Timeout);

        try
        {
            await processRunner.RunProcessAsync(["docker", "compose", "up"], jobModel.Directory,
                jobTimeoutCancellation.Token);
            jobModel.Status = JobStatus.Finished;
        }
        catch (OperationCanceledException e)
        {
            logger.LogWarning(e, "Job [{JobId}] process was cancelled by Timeout", jobModel.Id);
            jobModel.Status = JobStatus.Timeout;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while running process for Job [{JobId}]", jobModel.Id);
            jobModel.Status = JobStatus.Fault;
        }
        finally
        {
            await ClearContainerAsync(jobModel);
            logger.LogCritical("Process for Job [{JobId}] ended with status [{JobStatus}]",
                jobModel.Id, jobModel.Status);
        }
    }

    private async Task ClearContainerAsync(RunJobModel jobModel)
    {
        try
        {
            await processRunner.RunProcessAsync(["docker", "compose", "down", "-t", "10"], jobModel.Directory, default);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while stopping Job [{JobId}] process", jobModel.Id);
        }
    }
}
