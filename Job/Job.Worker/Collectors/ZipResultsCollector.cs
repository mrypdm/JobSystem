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
    public async Task CollectResults(RunJobModel jobModel)
    {
        logger.LogInformation("Collecting Job [{JobId}] results", jobModel.Id);

        await processRunner.RunProcessAsync(["zip", "results.zip", "stdout.txt", "stderr.txt"], jobModel.Directory,
            default);

        jobModel.Results = await File.ReadAllBytesAsync(Path.Combine(jobModel.Directory, "results.zip"));

        logger.LogInformation("Job [{JobId}] results collected [{ResultsSize} MB]",
            jobModel.Id, jobModel.Results.LongLength / 1024.0 / 1024.0);
    }
}
