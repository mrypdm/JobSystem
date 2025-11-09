using User.Database.Models;

namespace User.Database.Contexts;

/// <summary>
/// Context for jobs
/// </summary>
public interface IUserDbContext
{
    /// <summary>
    /// Add new user
    /// </summary>
    Task AddNewUserAsync(UserDbModel user, CancellationToken cancellationToken);

    /// <summary>
    /// Get user
    /// </summary>
    Task<UserDbModel> GetUserAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// Add new user job
    /// </summary>
    Task AddNewUserJobAsync(string username, Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Get all user jobs
    /// </summary>
    Task<Guid[]> GetUserJobsAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// If Job belongs to user
    /// </summary>
    Task<bool> IsUserJobAsync(string username, Guid jobId, CancellationToken cancellationToken);
}
