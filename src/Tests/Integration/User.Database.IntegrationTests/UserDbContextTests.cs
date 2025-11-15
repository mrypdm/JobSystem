using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;
using Tests.Common;
using Tests.Common.Initializers;
using User.Database.Contexts;
using User.Database.Models;

namespace User.Database.IntegrationTests;

/// <summary>
/// Tests for <see cref="UserDbContext"/>
/// </summary>
internal class UserDbContextTests : IntegrationTestBase
{
    [Test]
    public async Task AddNewUser_ShouldAddUser()
    {
        // arrange
        var expectedUser = CreateTestUser();
        using var context = Services.GetRequiredService<UserDbContext>();

        // act
        await context.AddNewUserAsync(expectedUser, default);

        // assert
        var actualUser = context.Users.SingleOrDefault(m => m.Username == expectedUser.Username);

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
        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser, default);

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(() => context.AddNewUserAsync(expectedUser, default));
    }

    [Test]
    public async Task GetUser_ShouldReturnUser()
    {
        // arrange
        var expectedUser = CreateTestUser();
        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser, default);

        // act
        var actualUser = await context.GetUserAsync(expectedUser.Username, default);

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
        using var context = Services.GetRequiredService<UserDbContext>();

        // act
        var actualUser = await context.GetUserAsync("not_exist", default);

        // assert
        Assert.That(actualUser, Is.Null);
    }

    [Test]
    public async Task AddNewUserJob_ShouldAddUserJob()
    {
        // arrange
        var expectedUser = CreateTestUser();
        var expectedJob = Guid.NewGuid();

        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser, default);

        // act
        await context.AddNewUserJobAsync(expectedUser.Username, expectedJob, default);

        // assert
        var actualJob = context.UsersJobs.SingleOrDefault(m => m.JobId == expectedJob);

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

        using var context = Services.GetRequiredService<UserDbContext>();

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => context.AddNewUserJobAsync("not_exist", expectedJob, default));

        // assert
        var actualJob = context.UsersJobs.SingleOrDefault(m => m.JobId == expectedJob);

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

        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser1, default);
        await context.AddNewUserAsync(expectedUser2, default);
        await context.AddNewUserJobAsync(expectedUser1.Username, expectedJob1, default);
        await context.AddNewUserJobAsync(expectedUser1.Username, expectedJob2, default);

        // act
        var userJobs1 = await context.GetUserJobsAsync(expectedUser1.Username, default);
        var userJobs2 = await context.GetUserJobsAsync(expectedUser2.Username, default);

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
        using var context = Services.GetRequiredService<UserDbContext>();

        // act
        var userJobs = await context.GetUserJobsAsync("not_exists", default);

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

        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser, default);
        await context.AddNewUserJobAsync(expectedUser.Username, expectedJob, default);

        // act
        var result = await context.IsUserJobAsync(expectedUser.Username, expectedJob, default);

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

        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser1, default);
        await context.AddNewUserJobAsync(expectedUser1.Username, expectedJob, default);

        // act
        var result = await context.IsUserJobAsync(expectedUser2.Username, expectedJob, default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserJob_JobNotExists_ShouldReturnFalse()
    {
        // arrange
        var expectedUser = CreateTestUser();

        using var context = Services.GetRequiredService<UserDbContext>();
        await context.AddNewUserAsync(expectedUser, default);

        // act
        var result = await context.IsUserJobAsync(expectedUser.Username, Guid.Empty, default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserJob_UserNotExists_ShouldReturnFalse()
    {
        // arrange
        using var context = Services.GetRequiredService<UserDbContext>();

        // act
        var result = await context.IsUserJobAsync("not_exist", Guid.Empty, default);

        // assert
        Assert.That(result, Is.False);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>();
        var sslValidator = new SslValidator(dbOptions);
        builder.Services.AddDbContext<UserDbContext>(
            options => PostgreDbContext.BuildOptions(options, dbOptions, sslValidator),
            ServiceLifetime.Transient);
        builder.Services.AddScoped<IInitializer>(
            context => new DbInitializer(context.GetRequiredService<UserDbContext>()));
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
