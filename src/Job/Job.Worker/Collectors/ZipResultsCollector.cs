using Job.Worker.Environments;
using Job.Worker.Models;
using Job.Worker.Processes;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Collectors;

/// <summary>
/// Collects artifacts to ZIP archive
/// </summary>
public class ZipResultsCollector(IProcessRunner processRunner, ILogger<ZipResultsCollector> logger)
    : IResultsCollector
{
    /// <inheritdoc />
    public async Task CollectResultsAsync(RunJobModel jobModel)
    {
        ArgumentNullException.ThrowIfNull(jobModel);
        if (string.IsNullOrWhiteSpace(jobModel.Directory))
        {
            throw new InvalidOperationException(
                $"Cannot collect results of Job '{jobModel.Id}' because its environment is not initialized");
        }

        logger.LogInformation("Collecting Job [{JobId}] results", jobModel.Id);

        await processRunner.RunProcessAsync(
            ["zip", Constants.JobResultsFileName, Constants.StdOutFileName, Constants.StdErrFileName],
            jobModel.Directory,
            default);

        jobModel.Results = await File.ReadAllBytesAsync(Path.Combine(jobModel.Directory, Constants.JobResultsFileName));

        logger.LogInformation("Job [{JobId}] results collected [{ResultsSize} MB]",
            jobModel.Id, jobModel.Results.LongLength / 1024.0 / 1024.0);
    }
}
