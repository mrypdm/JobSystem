namespace Job.Worker.Resources.Models;

/// <summary>
/// Record for memory stats
/// </summary>
public record MemStat(long TotalMemory, long AvailableMemory)
{
    /// <summary>
    /// Percentage of used RAM
    /// </summary>
    public double UsagePercetage => 1 - (double)AvailableMemory / TotalMemory;
}
