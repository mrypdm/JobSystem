using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using User.Database.Contexts;
using User.Database.Models;
using User.WebApp.Extensions;
using User.WebApp.Models;

namespace User.WebApp.Controllers.Api;

/// <summary>
/// Controller for user authentication
/// </summary>
[AllowAnonymous]
[Route("api/auth")]
[ValidateAntiForgeryToken]
public class AuthenticationApiController(
    IUserDbContext userDbContext,
    ILogger<AuthenticationApiController> logger,
    IMemoryCache blockedUsersCache)
    : Controller
{
    /// <summary>
    /// Sign in user with cookie
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SignInAsync([FromForm] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Login request cannot be null");
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Both Username and Password should be provided");
        }

        var attempt = blockedUsersCache.Get<int>(request.Username);
        if (attempt++ >= 5)
        {
            return StatusCode((int)HttpStatusCode.TooManyRequests);
        }

        var user = await userDbContext.GetUserAsync(request.Username, cancellationToken);

        var salt = user is null
            ? RandomNumberGenerator.GetBytes(128 / 8)
            : Convert.FromBase64String(user.PasswordSalt);
        var hash = KeyDerivation.Pbkdf2(request.Password, salt, KeyDerivationPrf.HMACSHA512,
            iterationCount: 100000, numBytesRequested: 512 / 8);

        var userModel = new UserDbModel
        {
            Username = request.Username,
            PasswordHash = Convert.ToBase64String(hash),
            PasswordSalt = Convert.ToBase64String(salt)
        };

        if (user is null)
        {
            await userDbContext.AddNewUserAsync(userModel, cancellationToken);
        }
        else if (userModel.PasswordHash != user.PasswordHash)
        {
            blockedUsersCache.Set(request.Username, attempt, absoluteExpirationRelativeToNow: TimeSpan.FromHours(1));
            return Unauthorized("Wrong username and/or password");
        }

        blockedUsersCache.Remove(request.Username);
        await HttpContext.SignInAsync(request.Username);
        logger.LogCritical("User [{Username}] signed in", request.Username);

        return Ok();
    }

    /// <summary>
    /// Sign out user with cookie
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> SignOutAsync()
    {
        var userName = HttpContext.GetUserName();
        if (userName is null)
        {
            logger.LogCritical("Anonymous request for sign out");
        }
        else
        {
            logger.LogCritical("User [{Username}] signed out", userName);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}
