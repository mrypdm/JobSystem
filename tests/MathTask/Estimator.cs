namespace MathTask;

/// <summary>
/// Metric of resources
/// </summary>
public record Metric(TimeSpan Time, long CpuUsage, long RamUsage, long RunningJobs, long TotalJobs);

/// <summary>
/// Estimator of resources
/// </summary>
public class Estimator(long cpuCores, long ramGb)
{
    private long _currentRamUsage = 0;
    private long _currentCpuUsage = 0;
    private long _runningJobs = 0;
    private long _totalJobs = 0;

    private readonly long _cpuTime = cpuCores * 100;
    private readonly long _ramBytes = ramGb << 30;

    /// <summary>
    /// Estimator max CPU cores
    /// </summary>
    public long CpuCores => cpuCores;

    /// <summary>
    /// Estimator max RAM GB
    /// </summary>
    public long RamGb => ramGb;

    /// <summary>
    /// Add new Job to queue
    /// </summary>
    public void CreateJob()
    {
        _totalJobs++;
    }

    /// <summary>
    /// If Job can be started
    /// </summary>
    public bool CanRunJob(Job job)
    {
        return _currentCpuUsage + job.CpuUsage <= _cpuTime && _currentRamUsage + job.RamUsage <= _ramBytes;
    }

    /// <summary>
    /// Start Job
    /// </summary>
    public void StartJob(Job job)
    {
        _runningJobs++;
        _currentCpuUsage += job.CpuUsage;
        _currentRamUsage += job.RamUsage;
    }

    /// <summary>
    /// Finish Job
    /// </summary>
    public void FinishJob(Job job)
    {
        _runningJobs--;
        _totalJobs--;
        _currentCpuUsage -= job.CpuUsage;
        _currentRamUsage -= job.RamUsage;
    }

    /// <summary>
    /// Get current resources metrics
    /// </summary>
    public Metric GetStat(TimeSpan time)
    {
        return new Metric(time, _currentCpuUsage, _currentRamUsage, _runningJobs, _totalJobs);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"CPU={CpuCores} RAM={RamGb}";
    }
}
