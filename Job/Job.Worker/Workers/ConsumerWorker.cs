using Confluent.Kafka;
using Job.Broker;
using Job.Broker.Consumers;
using Job.Database.Contexts;
using Job.Worker.Models;
using Job.Worker.Options;
using Job.Worker.Resources.Analyzers;
using Job.Worker.Runners;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Job.Worker.Workers;

/// <summary>
/// Worker for Jobs
/// </summary>
public class ConsumerWorker(
    IJobConsumer<Guid, JobMessage> consumer,
    IJobRunner runner,
    IResourcesAnalyzer resourceMonitor,
    IJobDbContext jobsDbContext,
    ConsumerWorkerOptions consumerWorkerOptions,
    ILogger<ConsumerWorker> logger)
    : IHostedService
{
    private readonly CancellationTokenSource _consumingLoopCancellation = new();
    private Task _consumingLoopTask;

    private ConsumeResult<Guid, JobMessage> _lastConsumed;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting consuming Jobs");

        consumer.Subscribe();
        _consumingLoopTask = Task.Run(
            () => ConsumingLooop(_consumingLoopCancellation.Token),
            _consumingLoopCancellation.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Cancelling consuming Jobs");
        _consumingLoopCancellation.Cancel();
        await _consumingLoopTask;
        consumer.Dispose();
        logger.LogInformation("Consuming ended");

        logger.LogInformation("Waiting for Jobs to finish");
        await runner.WaitForAllJobs();
        logger.LogInformation("All Jobs finished");
    }

    private async Task ConsumingLooop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Consume iteration started");

            if (await resourceMonitor.CanRunNewJobAsync(cancellationToken))
            {
                await ConsumeOnceAsync(cancellationToken);
            }
            else
            {
                logger.LogInformation("Consuming skipped because there are no resources for new Job");
            }

            logger.LogDebug("Consume iteration ended");

            try
            {
                logger.LogDebug("Sleeping for [{IterationDeplay}]", consumerWorkerOptions.IterationDeplay);
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

            var job = await jobsDbContext.GetNewJobAsync(_lastConsumed.Message.Value.Id, cancellationToken);
            logger.LogInformation("Job [{JobId}] loaded from database", _lastConsumed.Message.Value.Id);

            var needToRun = await SetJobAsRunning(job.Id, cancellationToken);

            if (needToRun)
            {
                runner.RunJob(new RunJobModel
                {
                    Id = job.Id,
                    Timeout = job.Timeout,
                    Script = job.Script
                });
            }

            consumer.Commit(_lastConsumed);
            _lastConsumed = null;
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

    private async Task<bool> SetJobAsRunning(Guid jobId, CancellationToken cancellationToken)
    {
        try
        {
            await jobsDbContext.SetJobRunningAsync(_lastConsumed.Message.Value.Id, cancellationToken);
        }
        catch (PostgresException e) when (e.MessageText == "Job is running")
        {
            // NOP
        }
        catch (PostgresException e) when (e.MessageText.Contains("Job is finished"))
        {
            logger.LogWarning("Job is finished already. Skipping it");
            return false;
        }

        return true;
    }
}
