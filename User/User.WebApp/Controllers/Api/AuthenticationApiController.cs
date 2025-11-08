using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
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
public class AuthenticationApiController(IUserDbContext userDbContext) : Controller
{
    /// <summary>
    /// Sign in user with cookie
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SignInAsync([FromForm] LoginRequest request, CancellationToken cancellationToken)
    {
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
            return StatusCode(403, "Wrong username and/or password");
        }

        await HttpContext.SignInAsync(userModel);
        return Redirect(request.ReturnUrl);
    }

    /// <summary>
    /// Sign out user with cookie
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> SignOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}
