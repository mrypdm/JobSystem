using System.Diagnostics;
using Job.Contract;
using Job.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Job.Worker.JobProcesses;

/// <summary>
/// Run Job process in Docker
/// </summary>
public class DockerJobProcessRunner(ILogger<DockerJobProcessRunner> logger) : IJobProcessRunner
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
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("docker", ["compose", "up"])
            {
                WorkingDirectory = jobModel.Directory
            },
        };
        process.Start();

        logger.LogCritical("Process for Job [{JobId}] started with timeout [{Timeout}]",
            jobModel.Id, jobModel.Timeout);

        try
        {
            await process.WaitForExitAsync(jobTimeoutCancellation.Token);
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
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("docker", ["compose", "down", "-t", "10"])
                {
                    WorkingDirectory = jobModel.Directory
                },
            };
            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while stopping Job [{JobId}] process", jobModel.Id);
        }
    }
}
