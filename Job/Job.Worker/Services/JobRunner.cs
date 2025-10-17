using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Job.Database.Contexts;
using Job.Worker.Models;
using Job.Worker.Options;
using Microsoft.Extensions.Logging;
using Shared.Contract.Models;

namespace Job.Worker.Services;

/// <summary>
/// Runner of Jobs
/// </summary>
public class JobRunner(JobsDbContext jobsDbContext, JobRunnerOptions options, ILogger<JobRunner> logger)
{
    private readonly ConcurrentDictionary<Guid, Task> _jobs = [];

    /// <summary>
    /// Waiting for all Jobs to complete
    /// </summary>
    public async Task WaitForAllJobs()
    {
        await Task.WhenAll(_jobs.Values);
        _jobs.Clear();
    }

    /// <summary>
    /// Run Job
    /// </summary>
    public void RunJob(RunJobModel runJobModel, CancellationToken cancellationToken)
    {
        var jobTask = RunJobAsync(runJobModel, cancellationToken);
        _jobs.TryAdd(runJobModel.Id, jobTask);
    }

    private async Task RunJobAsync(RunJobModel runJobModel, CancellationToken cancellationToken)
    {
        try
        {
            runJobModel.Directory = PrepareJobEnvironment(runJobModel);
            await RunProcessAsync(runJobModel, cancellationToken);
            await FinishJobAsync(runJobModel, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while running Job [{JobId}]", runJobModel.Id);
        }
        finally
        {
            ClearJobEnvironment(runJobModel.Directory);
        }
    }

    private async Task RunProcessAsync(RunJobModel runJobModel, CancellationToken cancellationToken)
    {
        using var jobTimeoutCancellation = new CancellationTokenSource(runJobModel.Timeout);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("docker", ["compose", "up"])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        process.Start();

        logger.LogCritical("Process for Job [{JobId}] started with timeout [{Timeout}]",
            runJobModel.Id, runJobModel.Timeout);

        using var stdout = File.OpenWrite(Path.Combine(runJobModel.Directory, "stdout.txt"));
        using var stderr = File.OpenWrite(Path.Combine(runJobModel.Directory, "stderr.txt"));
        var logsTask = Task.WhenAll(
            process.StandardOutput.BaseStream.CopyToAsync(stdout),
            process.StandardError.BaseStream.CopyToAsync(stderr));

        try
        {
            await process.WaitForExitAsync(jobTimeoutCancellation.Token);
            runJobModel.Status = JobStatus.Finished;
        }
        catch (OperationCanceledException e)
        {
            logger.LogWarning(e, "Process was cancelled by [{Reason}]",
                jobTimeoutCancellation.IsCancellationRequested ? "Timeout" : "External");

            process.Kill(entireProcessTree: true);
            runJobModel.Status = jobTimeoutCancellation.IsCancellationRequested ? JobStatus.Timeout : JobStatus.Fault;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while running process for Job [{JobId}]", runJobModel.Id);
            runJobModel.Status = JobStatus.Fault;
        }
        finally
        {
            await logsTask;
            logger.LogCritical("Process for Job [{JobId}] ended with status [{JobStatus}]",
                runJobModel.Id, runJobModel.Status);
        }
    }

    private async Task FinishJobAsync(RunJobModel runJobModel, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("zip", ["results.zip", "stdout.txt", "stderr.txt"])
            {
                WorkingDirectory = runJobModel.Directory
            }
        };

        logger.LogInformation("Collecting Job [{JobId}] results", runJobModel.Id);

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        runJobModel.Results = await File.ReadAllBytesAsync(
            Path.Combine(runJobModel.Directory, "results.zip"),
            cancellationToken);

        logger.LogInformation("Job [{JobId}] results collected [{ResultsSize} MB]",
            runJobModel.Id, runJobModel.Results.LongLength / 1024.0 / 1024.0);

        await jobsDbContext.SetJobResultsAsync(runJobModel.Id, runJobModel.Status, runJobModel.Results,
            cancellationToken);

        _jobs.TryRemove(runJobModel.Id, out _);
    }

    private string GetJobDirectory(Guid jobId)
    {
        return Path.Join(options.JobsDirectory, jobId.ToString());
    }

    private string PrepareJobEnvironment(RunJobModel runJobModel)
    {
        var jobDirectory = GetJobDirectory(runJobModel.Id);
        Directory.Delete(jobDirectory, true);

        var dockerFile = File.ReadAllText("job.template")
            .Replace("<JOB_ID>", runJobModel.Id.ToString())
            .Replace("<JOB_DIR>", jobDirectory);

        Directory.CreateDirectory(jobDirectory);
        File.WriteAllText(Path.Combine(jobDirectory, "docker-compose.yaml"), dockerFile);

        var scriptFile = Path.Combine(jobDirectory, "run.sh");
        using var file = File.OpenWrite(scriptFile);
        using var base64Stream = new CryptoStream(file, new FromBase64Transform(), CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(base64Stream);
        streamWriter.Write(runJobModel.Script);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            File.SetUnixFileMode(scriptFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
        }

        logger.LogInformation("Environment [{JobEnvironment}] prepared", jobDirectory);

        return jobDirectory;
    }

    private void ClearJobEnvironment(string jobDirectory)
    {
        Directory.Delete(jobDirectory, true);
        logger.LogInformation("Environment [{JobEnvironment}] cleared", jobDirectory);
    }
}
