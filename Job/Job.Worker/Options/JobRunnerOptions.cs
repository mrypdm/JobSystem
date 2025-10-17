using Job.Worker.Workers;

namespace Job.Worker.Options;

/// <summary>
/// Options for <see cref="JobRunner"/>
/// </summary>
public class JobRunnerOptions
{
    /// <summary>
    /// Directory for Jobs files
    /// </summary>
    public string JobsDirectory { get; set; }
}
