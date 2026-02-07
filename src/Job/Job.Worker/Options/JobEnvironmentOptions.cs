using Job.Worker.Environments;

namespace Job.Worker.Options;

/// <summary>
/// Options for <see cref="IJobEnvironment"/>
/// </summary>
public class JobEnvironmentOptions
{
    /// <summary>
    /// Directory for Jobs files
    /// </summary>
    public string JobsDirectory { get; set; }

    /// <summary>
    /// CPU usage of one Job
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Memory usage of one Job in MB
    /// </summary>
    public long MemoryUsage { get; set; }
}
