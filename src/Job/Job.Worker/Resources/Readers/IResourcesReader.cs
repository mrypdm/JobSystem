using Job.Worker.Resources.Models;

namespace Job.Worker.Resources.Readers;

/// <summary>
/// Reader for system resource
/// </summary>
public interface IResourcesReader
{
    /// <summary>
    /// Get statistics for CPU
    /// </summary>
    Task<CpuStat> GetCpuStatisticsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get statistics for RAM
    /// </summary>
    Task<MemStat> GetRamStatisticsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get statistics for Drive
    /// </summary>
    Task<DriveStat> GetDriveStatisticsAsync(string path, CancellationToken cancellationToken);
}
