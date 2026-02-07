using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using User.Database.Models;

namespace User.WebApp.Extensions;

/// <inheritdoc />
public static class HttpContextExtensions
{
    /// <summary>
    /// Claim for user IP address
    /// </summary>
    public const string IpAddressClaim = "IpAddress";

    /// <summary>
    /// Sign in user with cookie
    /// </summary>
    public static async Task SignInAsync(this HttpContext context, string username)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new(IpAddressClaim, context.GetUserIpAddress()),
                new(ClaimTypes.Name, username)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    /// <summary>
    /// Get user IP address
    /// </summary>
    public static string GetUserIpAddress(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString()
            ?? throw new InvalidOperationException("Cannot determine IP of user");
    }

    /// <summary>
    /// Get name of current user
    /// </summary>
    public static string GetUserName(this HttpContext context)
    {
        return context.User.Claims.SingleOrDefault(m => m.Type == ClaimTypes.Name)?.Value;
    }
}
