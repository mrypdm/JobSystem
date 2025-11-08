namespace Job.Worker.Monitors;

/// <summary>
/// Record for memory stats
/// </summary>
public record MemStat(long TotalMemory, long AvailableMemory, double Usage);
