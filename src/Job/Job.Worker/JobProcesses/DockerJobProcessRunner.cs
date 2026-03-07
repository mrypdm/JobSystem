using Docker.DotNet;
using Docker.DotNet.Models;
using Job.Worker.Models;
using Job.Worker.Options;
using Serilog;
using Shared.Contract.Extensions;
using Shared.Contract.Owned;

using JobStatus = Job.Contract.JobStatus;

namespace Job.Worker.JobProcesses;

/// <summary>
/// Run Job process in Docker
/// </summary>
public class DockerJobProcessRunner(
    IOwnedService<IDockerClient> dockerClientOwned,
    JobEnvironmentOptions jobEnvironmentOptions,
    ILogger logger)
    : IJobProcessRunner
{
    private readonly ContainerStartParameters _containerStartParameters = new();
    private readonly ContainerStopParameters _containerStopParameters = new()
    {
        WaitBeforeKillSeconds = 10
    };
    private readonly ContainerRemoveParameters _containerRemoveParameters = new()
    {
        Force = true
    };

    private readonly ILogger _logger = logger.ForContext<DockerJobProcessRunner>();

    /// <inheritdoc />
    public async Task RunProcessAsync(RunJobModel jobModel)
    {
        ArgumentNullException.ThrowIfNull(jobModel);
        if (string.IsNullOrWhiteSpace(jobModel.Directory))
        {
            throw new InvalidOperationException(
                $"Cannot run process for Job '{jobModel.Id}' because its environment is not initialized");
        }
        if (jobModel.Status > JobStatus.Running)
        {
            throw new InvalidOperationException(
                $"Cannot run process for Job '{jobModel.Id}' because it is marked as '{jobModel.Status}'");
        }

        using var jobTimeoutCancellation = new CancellationTokenSource(jobModel.Timeout);
        _logger.Critical().Information("Starting process for Job [{JobId}] with timeout [{Timeout}]",
            jobModel.Id, jobModel.Timeout);

        var dockerClient = dockerClientOwned.Value;
        string containerId = null;

        try
        {
            containerId = await CreateContainerAsync(dockerClient, jobModel, jobTimeoutCancellation.Token);
            await StartContainerAsync(dockerClient, containerId, jobModel.Id, jobTimeoutCancellation.Token);
            await dockerClient.Containers.WaitContainerAsync(containerId, jobTimeoutCancellation.Token);
            jobModel.Status = JobStatus.Finished;
        }
        catch (OperationCanceledException e)
        {
            _logger.Warning(e, "Job [{JobId}] process was cancelled by Timeout", jobModel.Id);
            jobModel.Status = JobStatus.Timeout;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error while running process for Job [{JobId}]", jobModel.Id);
            jobModel.Status = JobStatus.Fault;
        }
        finally
        {
            await ClearContainerAsync(dockerClient, containerId, jobModel.Id);
            _logger.Critical().Information("Process for Job [{JobId}] ended with status [{JobStatus}]",
                jobModel.Id, jobModel.Status);
        }
    }

    private async Task<string> CreateContainerAsync(IDockerClient dockerClient, RunJobModel jobModel,
        CancellationToken cancellationToken)
    {
        _logger.Critical().Information("Creating container for Job [{JobId}]", jobModel.Id);
        var containerCreateResult = await dockerClient.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = "alpine",
                Name = $"job_{jobModel.Id}",
                User = "10000:10000",
                Entrypoint = ["/bin/sh"],
                Cmd = ["-c", "/bin/sh /etc/job/run.sh 1>/dev/stdout 2>/dev/stderr"],
                HostConfig = new HostConfig
                {
                    NanoCPUs = (long)(jobEnvironmentOptions.CpuUsage * 1_000_000_000),
                    Memory = jobEnvironmentOptions.MemoryUsage * 1024 * 1024,
                    RestartPolicy = new RestartPolicy()
                    {
                        Name = RestartPolicyKind.No
                    },
                    Binds = [
                        $"{jobModel.Directory}/run.sh:/etc/job/run.sh:ro",
                        $"{jobModel.Directory}/stdout.txt:/dev/stdout",
                        $"{jobModel.Directory}/stderr.txt:/dev/stderr"
                    ],
                }
            },
            cancellationToken);

        if (containerCreateResult.Warnings.Count != 0)
        {
            _logger.Warning(
                "Warning while creating container for Job [{JobId}]: {@Warnings}",
                jobModel.Id, containerCreateResult.Warnings);
        }

        _logger.Critical().Information(
            "Container [{ContainerId}] for Job [{JobId}] created",
            containerCreateResult.ID, jobModel.Id);
        return containerCreateResult.ID;
    }

    private async Task StartContainerAsync(IDockerClient dockerClient, string containerId, Guid jobId,
        CancellationToken cancellationToken)
    {
        var containerStarted = await dockerClient.Containers.StartContainerAsync(
            containerId, _containerStartParameters, cancellationToken);

        if (!containerStarted)
        {
            throw new InvalidOperationException($"Failed to start container for Job [{jobId}]");
        }

        _logger.Critical().Information(
            "Container [{ContainerId}] for Job [{JobId}] started",
            containerId, jobId);
    }

    private async Task ClearContainerAsync(IDockerClient dockerClient, string containerId, Guid jobId)
    {
        try
        {
            await dockerClient.Containers.StopContainerAsync(containerId, _containerStopParameters, default);
            _logger.Critical().Information(
                "Container [{ContainerId}] for Job [{JobId}] stopped",
                containerId, jobId);

            await dockerClient.Containers.RemoveContainerAsync(containerId, _containerRemoveParameters, default);
            _logger.Critical().Information(
                "Container [{ContainerId}] for Job [{JobId}] removed",
                containerId, jobId);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error while stopping Job [{JobId}] process", jobId);
        }
    }
}
