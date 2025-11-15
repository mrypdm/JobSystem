namespace User.Database.Models;

/// <summary>
/// Model of User Job
/// </summary>
public class UserJobDbModel
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Id of Job
    /// </summary>
    public Guid JobId { get; set; }
}
