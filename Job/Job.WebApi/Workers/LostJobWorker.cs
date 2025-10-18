using System;
using System.Threading;
using System.Threading.Tasks;
using Job.Database.Contexts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Job.WebApi.Workers;

/// <summary>
/// Worker for finding Lost jobs
/// </summary>
public class LostJobWorker(
    JobsDbContext jobsDbContext,
    ILogger<LostJobWorker> logger,
    LostJobWorkerOptions options) : IHostedService
{
    private readonly CancellationTokenSource _wokerCancellation = new();
    private Task _workerTask;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _workerTask = RunLoopAsync(_wokerCancellation.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _wokerCancellation.Cancel();
        await _workerTask;
        _wokerCancellation.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Consume iteration started");
            await RunIterationAsync(cancellationToken);
            logger.LogDebug("Consume iteration ended");

            try
            {
                logger.LogDebug("Sleeping for [{IterationDeplay}]", options.IterationDeplay);
                await Task.Delay(options.IterationDeplay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // NOP
            }
        }
    }
    private async Task RunIterationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await jobsDbContext.MarkLostJobsAsync(options.LostTimeoutForJobs, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Consuming cancelled");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot consume message");
        }
    }
}
