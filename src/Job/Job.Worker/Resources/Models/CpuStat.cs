namespace Job.Worker.Resources.Models;

/// <summary>
/// Record for CPU stats
/// </summary>
/// <param name="Total">Total CPU time</param>
/// <param name="Idle">Idle CPU time</param>
public record CpuStat(long Total, long Idle);
