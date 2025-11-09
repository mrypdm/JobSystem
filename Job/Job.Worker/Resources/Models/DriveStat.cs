namespace Job.Worker.Resources.Models;

/// <summary>
/// Record for Drive stats
/// </summary>
public record DriveStat(long Total, long Free)
{
    /// <summary>
    /// Percentage of used disk space
    /// </summary>
    public double UsagePercentage => 1 - (double)Free / Total;
}
