using Job.Worker.Resources.Analyzers;

namespace Job.Worker.Options;

/// <summary>
/// Options for <see cref="IResourcesAnalyzer"/>
/// </summary>
public class ResourcesAnalyzerOptions
{
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
