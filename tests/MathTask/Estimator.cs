namespace MathTask;

/// <summary>
/// Metric of resources
/// </summary>
public record Metric(TimeSpan Time, long CpuUsage, long RamUsage, long RunningJobs, long TotalJobs);

/// <summary>
/// Estimator of resources
/// </summary>
public class Estimator(long cpu, long ram)
{
    private long _currentRamUsage = 0;
    private long _currentCpuUsage = 0;
    private long _runningJobs = 0;
    private long _totalJobs = 0;

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
        return _currentRamUsage + job.RamUsage <= ram && _currentCpuUsage + job.CpuUsage <= cpu;
    }

    /// <summary>
    /// Start Job
    /// </summary>
    public void StartJob(Job job)
    {
        _runningJobs++;
        _currentRamUsage += job.RamUsage;
        _currentCpuUsage += job.CpuUsage;
    }

    /// <summary>
    /// Finish Job
    /// </summary>
    public void FinishJob(Job job)
    {
        _runningJobs--;
        _totalJobs--;
        _currentRamUsage -= job.RamUsage;
        _currentCpuUsage -= job.CpuUsage;
    }

    /// <summary>
    /// Get current resources metrics
    /// </summary>
    public Metric GetStat(TimeSpan time)
    {
        return new Metric(time, _currentCpuUsage, _currentRamUsage, _runningJobs, _totalJobs);
    }
}
