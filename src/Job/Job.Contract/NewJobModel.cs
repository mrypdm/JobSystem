namespace Job.Contract;

/// <summary>
/// Model of New Job
/// </summary>
public class NewJobModel
{
    /// <summary>
    /// Job Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timeout of Job
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Script to run
    /// </summary>
    public string Script { get; set; }
}
