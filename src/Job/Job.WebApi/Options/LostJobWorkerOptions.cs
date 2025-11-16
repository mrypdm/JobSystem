using Job.WebApi.Workers;

namespace Job.WebApi.Options;

/// <summary>
/// Options for <see cref="LostJobWorker"/>
/// </summary>
public class LostJobWorkerOptions
{
    /// <summary>
    /// If worker is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Delay between consume tries
    /// </summary>
    public TimeSpan IterationDeplay { get; set; }

    /// <summary>
    /// Maxmium time for Job to be timeouted
    /// </summary>
    public TimeSpan LostTimeoutForJobs { get; set; }
}
