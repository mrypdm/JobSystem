using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Common;
using User.Database.Contexts;
using User.Database.Models;
using User.WebApp.Controllers.Api;
using User.WebApp.Extensions;
using User.WebApp.Models;

namespace Tests.Unit.User.WebApp;

/// <summary>
/// Tests for <see cref="AuthenticationApiController"/>
/// </summary>
[TestFixture]
internal class AuthenticationApiControllerTests : TestBase
{
    private readonly Mock<IUserDbContext> _userDbContext = new();
    private readonly Mock<IAuthenticationService> _authenticationService = new();

    [SetUp]
    public void SetUp()
    {
        _userDbContext.Reset();
        _authenticationService.Reset();
    }

    [Test]
    public async Task SignIn_NullRequest_ShouldReturnBadRequest()
    {
        // arrange
        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignInAsync(null, default);

        // assert
        Assert.That(response, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SignIn_EmptyUsername_ShouldReturnBadRequest()
    {
        // arrange
        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignInAsync(new LoginRequest(), default);

        // assert
        Assert.That(response, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SignIn_EmptyPassword_ShouldReturnBadRequest()
    {
        // arrange
        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignInAsync(new LoginRequest { Username = "not_empty" }, default);

        // assert
        Assert.That(response, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SignIn_UserNotExists_ShouldRegister()
    {
        // arrange
        var request = new LoginRequest
        {
            Username = "username",
            Password = "password"
        };
        UserDbModel actualUser = null;

        _userDbContext
            .Setup(m => m.GetUserAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDbModel)null);
        _userDbContext
            .Setup(m => m.AddNewUserAsync(It.IsAny<UserDbModel>(), It.IsAny<CancellationToken>()))
            .Callback((UserDbModel user, CancellationToken _) => actualUser = user);

        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignInAsync(request, default);

        // assert
        var salt = Convert.FromBase64String(actualUser.PasswordSalt);
        var hash = KeyDerivation.Pbkdf2(request.Password, salt, KeyDerivationPrf.HMACSHA512,
            iterationCount: 100000, numBytesRequested: 512 / 8);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(response, Is.TypeOf<OkResult>());
        Assert.That(actualUser.Username, Is.EqualTo(request.Username));
        Assert.That(actualUser.PasswordHash, Is.EqualTo(Convert.ToBase64String(hash)));
    }

    [Test]
    public async Task SignIn_UserExists_AndCorrectPassword_ShouldLogin()
    {
        // arrange
        var request = new LoginRequest
        {
            Username = "username",
            Password = "password"
        };

        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hash = KeyDerivation.Pbkdf2(request.Password, salt, KeyDerivationPrf.HMACSHA512,
            iterationCount: 100000, numBytesRequested: 512 / 8);
        var userModel = new UserDbModel
        {
            Username = request.Username,
            PasswordHash = Convert.ToBase64String(hash),
            PasswordSalt = Convert.ToBase64String(salt)
        };

        _userDbContext
            .Setup(m => m.GetUserAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userModel);

        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignInAsync(request, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(response, Is.TypeOf<OkResult>());
        _authenticationService.Verify(
            m => m.SignInAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(m =>
                    m.Claims.Count() == 2
                    && m.Claims.ElementAt(0).Type == HttpContextExtensions.IpAddressClaim
                    && m.Claims.ElementAt(0).Value == IPAddress.Loopback.ToString()
                    && m.Claims.ElementAt(1).Type == ClaimTypes.Name
                    && m.Claims.ElementAt(1).Value == request.Username),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Test]
    public async Task SignIn_UserExists_AndWrongPassword_ShouldReturnUnauthorized()
    {
        // arrange
        var request = new LoginRequest
        {
            Username = "username",
            Password = "password"
        };

        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hash = KeyDerivation.Pbkdf2("anotherPassword", salt, KeyDerivationPrf.HMACSHA512,
            iterationCount: 100000, numBytesRequested: 512 / 8);
        var userModel = new UserDbModel
        {
            Username = request.Username,
            PasswordHash = Convert.ToBase64String(hash),
            PasswordSalt = Convert.ToBase64String(salt)
        };

        _userDbContext
            .Setup(m => m.GetUserAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userModel);

        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignInAsync(request, default);

        // assert
        Assert.That(response, Is.TypeOf<UnauthorizedObjectResult>());
        _authenticationService.Verify(
            m => m.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Test]
    public async Task SignOut_ShouldSignOut()
    {
        // arrange
        using var controller = Services.GetRequiredService<AuthenticationApiController>();

        // act
        var response = await controller.SignOutAsync();

        // assert
        Assert.That(response, Is.TypeOf<OkResult>());
        _authenticationService.Verify(
            m => m.SignOutAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddTransient(context =>
        {
            var controller = new AuthenticationApiController(
                _userDbContext.Object,
                context.GetRequiredService<ILogger<AuthenticationApiController>>());

            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(m => m.GetService(typeof(IAuthenticationService)))
                .Returns(_authenticationService.Object);

            controller.ControllerContext.HttpContext.RequestServices = serviceProvider.Object;
            return controller;
        });
    }
}
