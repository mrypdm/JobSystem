using Job.Contract;

namespace Job.Database.Models;

/// <summary>
/// Full model of Job in Database
/// </summary>
public class JobDbModel
{
    /// <summary>
    /// Id of Job
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Status of Job
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Timeout of Job
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// When Job was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When Job was started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When Job was finished
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Script to Run
    /// </summary>
    public string Script { get; set; }

    /// <summary>
    /// Results of Job
    /// </summary>
    public byte[] Results { get; set; }
}
