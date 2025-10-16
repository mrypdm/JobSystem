using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace User.WebApp.Controllers;

/// <summary>
/// Controller for login view
/// </summary>
[AllowAnonymous]
[Route("auth")]
public class AuthenticationController : Controller
{
    /// <summary>
    /// Get login view
    /// </summary>
    [HttpGet("login")]
    public ActionResult GetLoginView([FromQuery] string returnUrl)
    {
        return View("Login");
    }

    /// <summary>
    /// Get logout view
    /// </summary>
    [HttpGet("logout")]
    public ActionResult GetLogoutView()
    {
        return View("Logout");
    }
}
