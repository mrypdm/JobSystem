using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tests.Common;
using User.Database.Contexts;
using User.WebApp.Controllers;
using User.WebApp.Models;

namespace User.WebApp.Tests;

/// <summary>
/// Tests for <see cref="JobController"/>
/// </summary>
[TestFixture]
internal class JobControllerTests : TestBase
{
    private const string Username = "username";

    private readonly Mock<IUserDbContext> _userDbContext = new();

    [SetUp]
    public void SetUp()
    {
        _userDbContext.Reset();
    }

    [Test]
    public async Task GetView_ShouldGetUserJobs()
    {
        // arrange
        var jobId = Guid.NewGuid();
        _userDbContext
            .Setup(m => m.GetUserJobsAsync(Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync([jobId]);

        using var controller = CreateController();

        // act
        var result = await controller.GetViewAsync(default);

        // assert
        Assert.That(result, Is.TypeOf<ViewResult>());
        Assert.That(((ViewResult)result).ViewName, Is.EqualTo("Index"));
        Assert.That(((ViewResult)result).Model, Is.EqualTo([jobId]).AsCollection);
    }

    [Test]
    public void GetJobResultsView_ShouldReturnView()
    {
        // arrange
        var jobId = Guid.NewGuid();
        using var controller = CreateController();

        // act
        var result = controller.GetJobResultsView(jobId);

        // assert
        Assert.That(result, Is.TypeOf<ViewResult>());
        Assert.That(((ViewResult)result).ViewName, Is.EqualTo("JobResults"));
        Assert.That(((ViewResult)result).Model, Is.EqualTo(jobId));
    }

    [Test]
    public void GetJobCreateView_ShouldReturnView()
    {
        // arrange
        using var controller = CreateController();

        // act
        var result = controller.GetJobCreateView();

        // assert
        Assert.That(result, Is.TypeOf<ViewResult>());
        Assert.That(((ViewResult)result).ViewName, Is.EqualTo("JobCreation"));
        Assert.That(((ViewResult)result).Model, Is.Not.Null);
        Assert.That(((ViewResult)result).Model, Is.TypeOf<CreateUserJobRequest>());

        var model = (CreateUserJobRequest)((ViewResult)result).Model;
        Assert.That(model.Timeout, Is.Default);
        Assert.That(model.Script, Is.Null);
    }

    private JobController CreateController()
    {
        var controller = new JobController(_userDbContext.Object);
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
    }
}
