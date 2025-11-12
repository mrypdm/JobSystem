using Job.WebApi.Controllers;

namespace Job.WebApi.Options;

/// <summary>
/// Options for <see cref="JobController"/>
/// </summary>
public class JobControllerOptions
{
    /// <summary>
    /// Default Job timeout
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; }

    /// <summary>
    /// Max Job timeout
    /// </summary>
    public TimeSpan MaxTimeout { get; set; }
}
