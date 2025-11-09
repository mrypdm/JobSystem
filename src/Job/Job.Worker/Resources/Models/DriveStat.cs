namespace Job.Worker.Resources.Models;

/// <summary>
/// Record for Drive stats
/// </summary>
/// <param name="Total">Total size of disk space in bytes</param>
/// <param name="Free">Size of free disk space in bytes</param>
public record DriveStat(long Total, long Free)
{
    /// <summary>
    /// Percentage of used disk space
    /// </summary>
    public double UsagePercentage => 1 - (double)Free / Total;
}
