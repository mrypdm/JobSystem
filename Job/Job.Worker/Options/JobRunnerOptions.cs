using Job.Worker.Runners;

namespace Job.Worker.Options;

/// <summary>
/// Options for <see cref="JobRunner"/>
/// </summary>
public class JobRunnerOptions
{
    /// <summary>
    /// Directory for Jobs files
    /// </summary>
    public string JobsDirectory { get; set; } = "/tmp/jobs";

    /// <summary>
    /// CPU usage of one Job
    /// </summary>
    public double CpuUsage { get; set; } = 0.5;

    /// <summary>
    /// Memory usage of one Job in MB
    /// </summary>
    public double MemoryUsage { get; set; } = 512;

    /// <summary>
    /// Maximum allowed percent of CPU usage
    /// </summary>
    public double ThresholdCpuUsage { get; set; } = 0.8;

    /// <summary>
    /// Maximum allowed percent of used memory
    /// </summary>
    public double ThresholdMemoryUsage { get; set; } = 0.8;

    /// <summary>
    /// Maximum allowed percent of used drive
    /// </summary>
    public double ThresholdDriveUsage { get; set; } = 0.8;

    /// <summary>
    /// Maximum allowd count of running Jobs
    /// </summary>
    public int ThresholdRunningJobs { get; set; } = 16;
}
