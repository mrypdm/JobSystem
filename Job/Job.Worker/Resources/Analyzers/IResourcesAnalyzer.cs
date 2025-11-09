namespace Job.Worker.Resources.Analyzers;

/// <summary>
/// Analyzer for system resources
/// </summary>
public interface IResourcesAnalyzer
{
    /// <summary>
    /// Check if new Job can be started
    /// </summary>
    Task<bool> CanRunNewJobAsync(CancellationToken cancellationToken);
}
