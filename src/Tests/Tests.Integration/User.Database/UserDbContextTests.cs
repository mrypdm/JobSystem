using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;
using Shared.Database.Migrations;
using User.Database.Contexts;
using User.Database.Models;

namespace Tests.Integration.User.Database;

/// <summary>
/// Tests for <see cref="UserDbContext"/>
/// </summary>
[TestFixture]
internal class UserDbContextTests : IntegrationTestBase
{
    private const string Default = nameof(Default);
    private const string Admin = nameof(Admin);

    [Test]
    public async Task AddNewUser_ShouldAddUser()
    {
        // arrange
        var expectedUser = CreateTestUser();

        // act
        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser, default);

        // assert
        using var adminContext = Services.GetRequiredKeyedService<UserDbContext>(Admin);
        var actualUser = adminContext.Users.SingleOrDefault(m => m.Username == expectedUser.Username);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualUser, Is.Not.Null);
        Assert.That(actualUser.Username, Is.EqualTo(expectedUser.Username));
        Assert.That(actualUser.PasswordHash, Is.EqualTo(expectedUser.PasswordHash));
        Assert.That(actualUser.PasswordSalt, Is.EqualTo(expectedUser.PasswordSalt));
    }

    [Test]
    public async Task AddNewUser_Duplicate_ShouldThrow()
    {
        // arrange
        var expectedUser = CreateTestUser();
        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser, default);

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(() => defaultContext.AddNewUserAsync(expectedUser, default));
    }

    [Test]
    public async Task GetUser_ShouldReturnUser()
    {
        // arrange
        var expectedUser = CreateTestUser();
        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser, default);

        // act
        var actualUser = await defaultContext.GetUserAsync(expectedUser.Username, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualUser, Is.Not.Null);
        Assert.That(actualUser.Username, Is.EqualTo(expectedUser.Username));
        Assert.That(actualUser.PasswordHash, Is.EqualTo(expectedUser.PasswordHash));
        Assert.That(actualUser.PasswordSalt, Is.EqualTo(expectedUser.PasswordSalt));
    }

    [Test]
    public async Task GetUser_UserNotExists_ShouldReturnNull()
    {
        // arrange
        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);

        // act
        var actualUser = await defaultContext.GetUserAsync("not_exist", default);

        // assert
        Assert.That(actualUser, Is.Null);
    }

    [Test]
    public async Task AddNewUserJob_ShouldAddUserJob()
    {
        // arrange
        var expectedUser = CreateTestUser();
        var expectedJob = Guid.NewGuid();

        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser, default);

        // act
        await defaultContext.AddNewUserJobAsync(expectedUser.Username, expectedJob, default);

        // assert
        using var adminContext = Services.GetRequiredKeyedService<UserDbContext>(Admin);
        var actualJob = adminContext.UsersJobs.SingleOrDefault(m => m.JobId == expectedJob);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob, Is.Not.Null);
        Assert.That(actualJob.Username, Is.EqualTo(expectedUser.Username));
        Assert.That(actualJob.JobId, Is.EqualTo(expectedJob));
    }

    [Test]
    public void AddNewUserJob_UserNotExists_ShouldThrow()
    {
        // arrange
        var expectedJob = Guid.NewGuid();

        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => defaultContext.AddNewUserJobAsync("not_exist", expectedJob, default));

        // assert
        using var adminContext = Services.GetRequiredKeyedService<UserDbContext>(Admin);
        var actualJob = adminContext.UsersJobs.SingleOrDefault(m => m.JobId == expectedJob);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob, Is.Null);
        Assert.That(exception.Message, Does.Contain("User not_exist does not exists"));
    }

    [Test]
    public async Task GetUserJobs_ShouldReturnUserJobs()
    {
        // arrange
        var expectedUser1 = CreateTestUser("first");
        var expectedUser2 = CreateTestUser("second");
        var expectedJob1 = Guid.NewGuid();
        var expectedJob2 = Guid.NewGuid();

        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser1, default);
        await defaultContext.AddNewUserAsync(expectedUser2, default);
        await defaultContext.AddNewUserJobAsync(expectedUser1.Username, expectedJob1, default);
        await defaultContext.AddNewUserJobAsync(expectedUser1.Username, expectedJob2, default);

        // act
        var userJobs1 = await defaultContext.GetUserJobsAsync(expectedUser1.Username, default);
        var userJobs2 = await defaultContext.GetUserJobsAsync(expectedUser2.Username, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(userJobs1, Is.Not.Null);
        Assert.That(userJobs1, Has.Length.EqualTo(2));
        Assert.That(userJobs1, Is.EqualTo([expectedJob1, expectedJob2]).AsCollection);
        Assert.That(userJobs2, Is.Not.Null);
        Assert.That(userJobs2, Is.Empty);
    }

    [Test]
    public async Task GetUserJobs_UserNotExists_ShouldReturnEmptyArray()
    {
        // arrange
        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);

        // act
        var userJobs = await defaultContext.GetUserJobsAsync("not_exists", default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(userJobs, Is.Not.Null);
        Assert.That(userJobs, Is.Empty);
    }

    [Test]
    public async Task IsUserJob_JobBelongsToUser_ShouldReturnTrue()
    {
        // arrange
        var expectedUser = CreateTestUser();
        var expectedJob = Guid.NewGuid();

        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser, default);
        await defaultContext.AddNewUserJobAsync(expectedUser.Username, expectedJob, default);

        // act
        var result = await defaultContext.IsUserJobAsync(expectedUser.Username, expectedJob, default);

        // assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsUserJob_JobNotBelongstoUser_ShouldReturnFalse()
    {
        // arrange
        var expectedUser1 = CreateTestUser("first");
        var expectedUser2 = CreateTestUser("second");
        var expectedJob = Guid.NewGuid();

        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser1, default);
        await defaultContext.AddNewUserJobAsync(expectedUser1.Username, expectedJob, default);

        // act
        var result = await defaultContext.IsUserJobAsync(expectedUser2.Username, expectedJob, default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserJob_JobNotExists_ShouldReturnFalse()
    {
        // arrange
        var expectedUser = CreateTestUser();

        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);
        await defaultContext.AddNewUserAsync(expectedUser, default);

        // act
        var result = await defaultContext.IsUserJobAsync(expectedUser.Username, Guid.Empty, default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserJob_UserNotExists_ShouldReturnFalse()
    {
        // arrange
        using var defaultContext = Services.GetRequiredKeyedService<UserDbContext>(Default);

        // act
        var result = await defaultContext.IsUserJobAsync("not_exist", Guid.Empty, default);

        // assert
        Assert.That(result, Is.False);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>("WebAppDatabaseOptions");
        var sslValidator = new SslValidator(dbOptions);
        builder.Services.AddKeyedTransient(Default, (context, _) =>
        {
            var options = PostgreDbContext.BuildOptions(
                new DbContextOptionsBuilder(),
                dbOptions,
                sslValidator,
                context.GetRequiredService<ILoggerFactory>(),
                forTests: true);
            return new UserDbContext(options.Options, context.GetRequiredService<ILogger<UserDbContext>>());
        });

        var adminDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("AdminUsersDatabaseOptions");
        var adminSslValidator = new SslValidator(adminDbOptions);
        builder.Services.AddKeyedTransient(Admin, (context, _) =>
        {
            var options = PostgreDbContext.BuildOptions(
                new DbContextOptionsBuilder(),
                adminDbOptions,
                adminSslValidator,
                context.GetRequiredService<ILoggerFactory>(),
                forTests: true);
            return new UserDbContext(options.Options, context.GetRequiredService<ILogger<UserDbContext>>());
        });
        builder.Services.AddTransient<IInitializer>(
            context => new DbInitializer(context.GetRequiredKeyedService<UserDbContext>(Admin)));
    }

    private static UserDbModel CreateTestUser(string username = "username")
    {
        return new UserDbModel
        {
            Username = username,
            PasswordHash = "pass",
            PasswordSalt = "salt"
        };
    }
}
