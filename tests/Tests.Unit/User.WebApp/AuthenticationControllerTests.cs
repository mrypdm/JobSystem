using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tests.Common;
using User.WebApp.Controllers;
using User.WebApp.Models;

namespace Tests.Unit.User.WebApp;

/// <summary>
/// Tests for <see cref="AuthenticationController"/>
/// </summary>
[TestFixture]
internal class AuthenticationControllerTests : TestBase
{
    [Test]
    [TestCase(null)]
    [TestCase("/url")]
    public void GetLoginView_ShouldReturnCorrectView(string expectedUrl)
    {
        // arrange
        expectedUrl ??= "/";
        using var controller = Services.GetRequiredService<AuthenticationController>();

        // act
        var result = controller.GetLoginView(expectedUrl);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(result, Is.TypeOf<ViewResult>());
        Assert.That(((ViewResult)result).ViewName, Is.EqualTo("Login"));
        Assert.That(((ViewResult)result).Model, Is.TypeOf<LoginRequest>());

        var model = ((ViewResult)result).Model as LoginRequest;
        Assert.That(model.ReturnUrl, Is.EqualTo(expectedUrl));
    }
    [Test]
    public void GetLogoutView_ShouldReturnCorrectView()
    {
        // arrange
        using var controller = Services.GetRequiredService<AuthenticationController>();

        // act
        var result = controller.GetLogoutView();

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(result, Is.TypeOf<ViewResult>());
        Assert.That(((ViewResult)result).ViewName, Is.EqualTo("Logout"));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddTransient<AuthenticationController>();
    }
}
