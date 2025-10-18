namespace User.Database.Models;

/// <summary>
/// Model of user Job
/// </summary>
public class UserJobDbModel
{
    /// <summary>
    /// Name of user
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Id of Job
    /// </summary>
    public Guid JobId { get; set; }
}
