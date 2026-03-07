using Job.Database.Contexts;
using Job.WebApi.Options;
using Shared.Contract.Owned;
using ILogger = Serilog.ILogger;

namespace Job.WebApi.Workers;

/// <summary>
/// Worker for finding Lost jobs
/// </summary>
public class LostJobWorker(
    IOwnedService<IJobDbContext> jobDbContextOwned,
    ILogger logger,
    LostJobWorkerOptions options) : IHostedService
{
    private readonly ILogger _logger = logger.ForContext<LostJobWorker>();
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
            _logger.Information("Worker is disabled");
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
            _logger.Debug("Consume iteration started");
            await RunIterationAsync(cancellationToken);
            _logger.Debug("Consume iteration ended");

            try
            {
                _logger.Debug("Sleeping for [{IterationDeplay}]", options.IterationDeplay);
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
            _logger.Warning("Iteration cancelled");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Cannot mark Jobs as Lost");
        }
    }
}
