using System.Net;
using System.Security.Claims;
using Job.Contract;
using Job.WebApi.Client;
using Job.WebApi.Client.Exceptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Common;
using User.Database.Contexts;
using User.WebApp.Controllers.Api;
using User.WebApp.Models;

namespace User.WebApp.UnitTests;

/// <summary>
/// Tests for <see cref="JobApiController"/>
/// </summary>
[TestFixture]
internal class JobApiControllerTests : TestBase
{
    private const string Username = "username";

    private readonly Mock<IUserDbContext> _userDbContext = new();
    private readonly Mock<IJobWebApiClient> _jobApiClient = new();

    [SetUp]
    public void SetUp()
    {
        _userDbContext.Reset();
        _jobApiClient.Reset();
    }

    [Test]
    public async Task CreateNewJob_CommonWay_ShouldCreateJobAndSaveItToDatabase()
    {
        // arrange
        var scriptBytes = new byte[] { 0x00, 0x11 };
        var scriptString = Convert.ToBase64String(scriptBytes);
        using var stream = new MemoryStream(scriptBytes);
        var request = new CreateUserJobRequest
        {
            Timeout = TimeSpan.FromSeconds(10),
            Script = new FormFile(stream, 0, stream.Length, "file", "file")
        };

        var jobId = Guid.NewGuid();

        _jobApiClient
            .Setup(m => m.CreateNewJobAsync(
                It.Is<CreateJobRequest>(m => m.Timeout == request.Timeout && m.Script == scriptString),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobId);

        using var controller = Services.GetRequiredService<JobApiController>();

        // act
        var response = await controller.CreateNewJobAsync(request, default);

        // assert
        Assert.That(response, Is.TypeOf<CreatedAtRouteResult>());
        _userDbContext.Verify(
            m => m.AddNewUserJobAsync(Username, jobId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void CreateNewJob_FailedToCreateJob_ShouldThrow()
    {
        // arrange
        using var stream = new MemoryStream([0x00, 0x11]);
        var request = new CreateUserJobRequest
        {
            Timeout = TimeSpan.FromSeconds(10),
            Script = new FormFile(stream, 0, stream.Length, "file", "file")
        };

        _jobApiClient
            .Setup(m => m.CreateNewJobAsync(It.IsAny<CreateJobRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobWebApiException(HttpStatusCode.BadRequest, "message", null));

        using var controller = Services.GetRequiredService<JobApiController>();

        // act
        var exception = Assert.ThrowsAsync<JobWebApiException>(() => controller.CreateNewJobAsync(request, default));

        // assert
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _userDbContext.Verify(
            m => m.AddNewUserJobAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void CreateNewJob_TimeoutWhileCreateJob_ShouldThrow()
    {
        // arrange
        using var stream = new MemoryStream([0x00, 0x11]);
        var request = new CreateUserJobRequest
        {
            Timeout = TimeSpan.FromSeconds(10),
            Script = new FormFile(stream, 0, stream.Length, "file", "file")
        };

        _jobApiClient
            .Setup(m => m.CreateNewJobAsync(It.IsAny<CreateJobRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobWebApiTimeoutException("message", null));

        using var controller = Services.GetRequiredService<JobApiController>();

        // act
        Assert.ThrowsAsync<JobWebApiTimeoutException>(() => controller.CreateNewJobAsync(request, default));

        // assert
        _userDbContext.Verify(
            m => m.AddNewUserJobAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetUserJobResults_CommonWay_ShouldCheckUserJobAndGetResults()
    {
        // arrange
        var jobId = Guid.NewGuid();
        var expectedResults = new JobResultResponse();
        _userDbContext
            .Setup(m => m.IsUserJobAsync(Username, jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _jobApiClient
            .Setup(m => m.GetJobResultsAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        using var controller = Services.GetRequiredService<JobApiController>();

        // act
        var response = await controller.GetUserJobResultsAsync(jobId, default);

        // assert
        Assert.That(response.Value, Is.SameAs(expectedResults));
    }

    [Test]
    public async Task GetUserJobResults_NotUserJob_ShouldReturnNotFound()
    {
        // arrange
        var jobId = Guid.NewGuid();
        _userDbContext
            .Setup(m => m.IsUserJobAsync(Username, jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        using var controller = Services.GetRequiredService<JobApiController>();

        // act
        var response = await controller.GetUserJobResultsAsync(jobId, default);

        // assert
        Assert.That(response.Result, Is.TypeOf<NotFoundObjectResult>());
        _jobApiClient.Verify(
            m => m.GetJobResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void GetUserJobResults_ResultsNotFound_ShouldThrow()
    {
        // arrange
        _userDbContext
            .Setup(m => m.IsUserJobAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _jobApiClient
            .Setup(m => m.GetJobResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobWebApiException(HttpStatusCode.NotFound, "message", null));

        using var controller = Services.GetRequiredService<JobApiController>();

        // act
        var exception = Assert.ThrowsAsync<JobWebApiException>(
            () => controller.GetUserJobResultsAsync(Guid.NewGuid(), default));

        // assert
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public void GetUserJobResults_TimeoutWhileGetResults_ShouldThrow()
    {
        // arrange
        _userDbContext
            .Setup(m => m.IsUserJobAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _jobApiClient
            .Setup(m => m.GetJobResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobWebApiTimeoutException("message", null));

        using var controller = Services.GetRequiredService<JobApiController>();

        // act & assert
        Assert.ThrowsAsync<JobWebApiTimeoutException>(
            () => controller.GetUserJobResultsAsync(Guid.NewGuid(), default));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddTransient(context =>
        {
            var controller = new JobApiController(_userDbContext.Object, _jobApiClient.Object);
            controller.ControllerContext.HttpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new(ClaimTypes.Name, Username)
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme))
            };
            return controller;
        });
    }
}
