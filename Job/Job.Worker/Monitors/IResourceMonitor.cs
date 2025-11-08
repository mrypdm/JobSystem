
namespace Job.Worker.Monitors;

/// <summary>
/// Monitor for system resources
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Load average load of CPU in percent
    /// </summary>
    Task<double> GetCpuLoadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get memory load in percent
    /// </summary>
    Task<MemStat> GetMemLoadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get drive load in percent
    /// </summary>
    Task<double> GetDriveLoad(string path);
}
