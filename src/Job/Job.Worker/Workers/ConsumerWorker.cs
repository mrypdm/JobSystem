using Confluent.Kafka;
using Job.Broker;
using Job.Database.Contexts;
using Job.Worker.Models;
using Job.Worker.Options;
using Job.Worker.Resources.Analyzers;
using Job.Worker.Runners;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using Shared.Broker.Abstractions;
using Shared.Contract.Owned;

namespace Job.Worker.Workers;

/// <summary>
/// Worker for Jobs
/// </summary>
public class ConsumerWorker(
    IBrokerConsumer<Guid, JobMessage> consumer,
    IJobRunner runner,
    IResourcesAnalyzer resourceAnalyzer,
    IOwnedService<IJobDbContext> jobDbContextOwned,
    ConsumerWorkerOptions consumerWorkerOptions,
    ILogger logger)
    : IHostedService
{
    private readonly ILogger _logger = logger.ForContext<ConsumerWorker>();

    private readonly CancellationTokenSource _consumingLoopCancellation = new();
    private Task _consumingLoopTask;

    private ConsumeResult<Guid, JobMessage> _lastConsumed;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Starting consuming Jobs");

        consumer.Subscribe();
        _consumingLoopTask = Task.Run(
            () => ConsumingLooop(_consumingLoopCancellation.Token),
            _consumingLoopCancellation.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Cancelling consuming Jobs");
        _consumingLoopCancellation.Cancel();
        await _consumingLoopTask;
        consumer.Dispose();
        _logger.Information("Consuming ended");

        _logger.Information("Waiting for Jobs to finish");
        await runner.WaitForAllJobs();
        _logger.Information("All Jobs finished");
    }

    private async Task ConsumingLooop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.Debug("Consume iteration started");

            if (await resourceAnalyzer.CanRunNewJobAsync(cancellationToken))
            {
                await ConsumeOnceAsync(cancellationToken);
            }
            else
            {
                _logger.Information("Consuming skipped because there are no resources for new Job");
            }

            _logger.Debug("Consume iteration ended");

            try
            {
                _logger.Debug("Sleeping for [{IterationDeplay}]", consumerWorkerOptions.IterationDeplay);
                await Task.Delay(consumerWorkerOptions.IterationDeplay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // NOP
            }
        }
    }

    internal async Task ConsumeOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            _lastConsumed ??= consumer.Consume(cancellationToken);

            using var jobDbContext = jobDbContextOwned.Value;
            var job = await jobDbContext.GetNewJobAsync(_lastConsumed.Message.Value.Id, cancellationToken);
            if (job is null)
            {
                _logger.Warning(
                    "Cannot find Job [{JobId}] for running (Job does not exists or has been already started)",
                    _lastConsumed.Message.Value.Id);
            }
            else
            {
                _logger.Information("Job [{JobId}] loaded from database", _lastConsumed.Message.Value.Id);
                runner.RunJob(new RunJobModel
                {
                    Id = job.Id,
                    Timeout = job.Timeout,
                    Script = job.Script
                });
                await SetJobAsRunningAsync(jobDbContext, job.Id, cancellationToken);
            }

            consumer.Commit(_lastConsumed);
            _lastConsumed = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Warning("Job processing was cancelled");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error processing new Job");
        }
    }

    private async Task SetJobAsRunningAsync(IJobDbContext jobDbContext, Guid jobId, CancellationToken cancellationToken)
    {
        try
        {
            await jobDbContext.SetJobRunningAsync(jobId, cancellationToken);
        }
        catch (PostgresException e) when (e.MessageText.Contains("Job is finished"))
        {
            _logger.Warning("Job is finished already. Skipping it");
        }
    }
}
