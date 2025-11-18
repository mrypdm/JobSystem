using Job.Database.Contexts;
using Job.WebApi.Options;
using Shared.Contract.Owned;

namespace Job.WebApi.Workers;

/// <summary>
/// Worker for finding Lost jobs
/// </summary>
public class LostJobWorker(
    IOwnedService<IJobDbContext> jobDbContextOwned,
    ILogger<LostJobWorker> logger,
    LostJobWorkerOptions options) : IHostedService
{
    private readonly CancellationTokenSource _wokerCancellation = new();
    private Task _workerTask;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _workerTask = options.IsEnabled
            ? RunLoopAsync(_wokerCancellation.Token)
            : Task.CompletedTask;

        if (!options.IsEnabled)
        {
            logger.LogInformation("Worker is disabled");
        }

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
            using var jobsDbContext = jobDbContextOwned.Value;
            await jobsDbContext.MarkLostJobsAsync(options.LostTimeoutForJobs, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Iteration cancelled");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot mark Jobs as Lost");
        }
    }
}
