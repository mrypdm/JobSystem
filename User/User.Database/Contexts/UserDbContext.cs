using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Contract.Options;
using User.Database.Models;

namespace User.Database.Contexts;

/// <summary>
/// Context for jobs
/// </summary>
public class UserDbContext(DbContextOptions options, ILogger<UserDbContext> logger) : DbContext(options)
{
    /// <summary>
    /// Table of <see cref="UserDbModel"/>
    /// </summary>
    public DbSet<UserDbModel> Users { get; set; }

    /// <summary>
    /// Table of <see cref="UserJobDbModel"/>
    /// </summary>
    public DbSet<UserJobDbModel> UserJobs { get; set; }

    /// <summary>
    /// Add new user
    /// </summary>
    public async Task AddNewUserAsync(UserDbModel user, CancellationToken cancellationToken)
    {
        await Database.ExecuteSqlAsync(
            $"call p_users_add_new_user({user.Username}, {user.PasswordHash}, {user.PasswordSalt})",
            cancellationToken);
        logger.LogCritical("User [{Username}] registered", user.Username);
    }

    /// <summary>
    /// Get user
    /// </summary>
    public async Task<UserDbModel> GetUserAsync(string username, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<UserDbModel>($"select * from f_users_get_user({username})")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Add new user job
    /// </summary>
    public async Task AddNewUserJobAsync(string username, Guid jobId, CancellationToken cancellationToken)
    {
        await Database.ExecuteSqlAsync(
            $"call p_users_add_new_job({username}, {jobId})",
            cancellationToken);
        logger.LogCritical("New Job [{JobId}] created by user [{Username}]", jobId, username);
    }

    /// <summary>
    /// Get all user jobs
    /// </summary>
    public async Task<Guid[]> GetUserJobsAsync(string username, CancellationToken cancellationToken)
    {
        return await Database
            .SqlQuery<Guid>($"select * from f_users_get_user_jobs({username})")
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// If Job belongs to user
    /// </summary>
    public async Task<bool> IsUserJobAsync(string username, Guid jobId, CancellationToken cancellationToken)
    {
        var res = await Database
            .SqlQuery<int>($"select * from f_users_check_user_job({username}, {jobId})")
            .SingleOrDefaultAsync(cancellationToken);
        return res == 1;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Create connection string for database
    /// </summary>
    public static string GetConnectionString(DatabaseOptions databaseOptions)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = databaseOptions.HostName,
            Port = databaseOptions.Port,
            Database = databaseOptions.DatabaseName,
            SslMode = SslMode.VerifyFull,
            Username = databaseOptions.UserName,
            RootCertificate = databaseOptions.TruststoreFilePath,
            SslCertificate = databaseOptions.CertificateFilePath,
            SslKey = databaseOptions.KeyFilePath,
            SslPassword = databaseOptions.Password,
        }.ConnectionString;
    }
}
