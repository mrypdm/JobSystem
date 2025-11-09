namespace Job.Worker.Resources.Models;

/// <summary>
/// Record for memory stats
/// </summary>
/// <param name="Total">Tital RAM size in MB</param>
/// <param name="Available">Available RAM size in MB</param>
public record MemStat(long Total, long Available)
{
    /// <summary>
    /// Percentage of used RAM
    /// </summary>
    public double UsagePercetage => 1 - (double)Available / Total;
}
