using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Database;
using User.Database.Models;

namespace User.Database.Contexts;

/// <inheritdoc />
public class UserDbContext(DbContextOptions options, ILogger<UserDbContext> logger) : PostgreDbContext(options), IUserDbContext
{
    /// <inheritdoc />
    public async Task AddNewUserAsync(UserDbModel user, CancellationToken cancellationToken)
    {
        await Database.ExecuteSqlAsync(
            $"call p_users_add_new_user({user.Username}, {user.PasswordHash}, {user.PasswordSalt})",
            cancellationToken);
        logger.LogCritical("User [{Username}] registered", user.Username);
    }

    /// <inheritdoc />
    public async Task<UserDbModel> GetUserAsync(string username, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<UserDbModel>($"select * from f_users_get_user({username})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddNewUserJobAsync(string username, Guid jobId, CancellationToken cancellationToken)
    {
        await Database.ExecuteSqlAsync(
            $"call p_users_add_new_job({username}, {jobId})",
            cancellationToken);
        logger.LogCritical("New Job [{JobId}] created by user [{Username}]", jobId, username);
    }

    /// <inheritdoc />
    public async Task<Guid[]> GetUserJobsAsync(string username, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<Guid>($"select * from f_users_get_user_jobs({username})")
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsUserJobAsync(string username, Guid jobId, CancellationToken cancellationToken)
    {
        var res = await Database
            .SqlQuery<Guid>($"select * from f_users_check_user_job({username}, {jobId})")
            .SingleOrDefaultAsync(cancellationToken);
        return res != Guid.Empty;
    }
}
