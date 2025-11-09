
namespace Job.Worker.Monitors;

/// <summary>
/// Monitor for system resources
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Check if new Job can be started
    /// </summary>
    Task<bool> CanRunNewJobAsync(CancellationToken cancellationToken);
}
