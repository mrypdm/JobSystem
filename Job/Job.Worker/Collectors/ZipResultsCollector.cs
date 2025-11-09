using System.Diagnostics;
using Job.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Collectors;

/// <summary>
/// Collects artifacts to ZIP archive
/// </summary>
public class ZipResultsCollector(ILogger<ZipResultsCollector> logger) : IResultsCollector
{
    /// <inheritdoc />
    public async Task CollectResults(RunJobModel jobModel)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("zip", ["results.zip", "stdout.txt", "stderr.txt"])
            {
                WorkingDirectory = jobModel.Directory
            }
        };

        logger.LogInformation("Collecting Job [{JobId}] results", jobModel.Id);

        process.Start();
        await process.WaitForExitAsync();

        jobModel.Results = await File.ReadAllBytesAsync(Path.Combine(jobModel.Directory, "results.zip"));

        logger.LogInformation("Job [{JobId}] results collected [{ResultsSize} MB]",
            jobModel.Id, jobModel.Results.LongLength / 1024.0 / 1024.0);
    }
}
