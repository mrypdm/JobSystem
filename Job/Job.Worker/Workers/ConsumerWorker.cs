using Job.Broker.Consumers;
using Job.Database.Contexts;
using Job.Worker.Models;
using Job.Worker.Options;
using Job.Worker.Runners;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Workers;

/// <summary>
/// Worker for Jobs
/// </summary>
public class ConsumerWorker(
    IJobConsumer consumer,
    IJobRunner runner,
    IJobDbContext jobsDbContext,
    ConsumerWorkerOptions consumerWorkerOptions,
    ILogger<ConsumerWorker> logger)
    : IHostedService
{
    private readonly CancellationTokenSource _consumingLoopCancellation = new();
    private Task _consumingLoopTask;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting consuming Jobs");

        consumer.Subscribe();
        _consumingLoopTask = ConsumingLooop(_consumingLoopCancellation.Token);

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

            if (await runner.CanRunNewJob(cancellationToken))
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

    private async Task ConsumeOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = consumer.Consume(cancellationToken);

            var job = await jobsDbContext.GetNewJobAsync(result.Message.Value.Id, cancellationToken);
            logger.LogInformation("Job [{JobId}] loaded from database", result.Message.Value.Id);

            await jobsDbContext.SetJobRunningAsync(result.Message.Value.Id, cancellationToken);

            runner.RunJob(new RunJobModel
            {
                Id = job.Id,
                Timeout = job.Timeout,
                Script = job.Script
            });

            consumer.Commit(result);
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
