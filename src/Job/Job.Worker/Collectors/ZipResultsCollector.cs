using System.IO.Compression;
using Job.Worker.Environments;
using Job.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Collectors;

/// <summary>
/// Collects artifacts to ZIP archive
/// </summary>
public class ZipResultsCollector(ILogger<ZipResultsCollector> logger)
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
        jobModel.Results = CreateZip(jobModel);
        logger.LogInformation("Job [{JobId}] results collected [{ResultsSize} MB]",
            jobModel.Id, jobModel.Results.LongLength / 1024.0 / 1024.0);
    }

    private static byte[] CreateZip(RunJobModel jobModel)
    {
        using var bytes = new MemoryStream();

        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.CreateEntryFromFile(Path.Combine(jobModel.Directory, Constants.StdOutFileName),
                Constants.StdOutFileName, CompressionLevel.Optimal);
            zip.CreateEntryFromFile(Path.Combine(jobModel.Directory, Constants.StdErrFileName),
                Constants.StdErrFileName, CompressionLevel.Optimal);
        }

        return bytes.ToArray();
    }
}
