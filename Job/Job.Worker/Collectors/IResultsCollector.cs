using Job.Worker.Models;

namespace Job.Worker.Collectors;

/// <summary>
/// Collector of results of Job
/// </summary>
public interface IResultsCollector
{
    /// <summary>
    /// Collect results of Job
    /// </summary>
    Task CollectResultsAsync(RunJobModel jobModel);
}
