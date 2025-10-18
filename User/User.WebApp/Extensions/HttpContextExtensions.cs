using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using User.Database.Models;
using User.WebApp.Extensions;

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
    public static async Task SignInAsync(this HttpContext context, UserDbModel user, bool withProxy)
    {
        var ip = context.GetUserIpAddress(withProxy)
            ?? throw new InvalidOperationException("Cannot determine IP of user");

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new(IpAddressClaim, ip),
                new(ClaimTypes.Name, user.Username)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    /// <summary>
    /// Get user IP address
    /// </summary>
    public static string GetUserIpAddress(this HttpContext context, bool withProxy)
    {
        if (withProxy)
        {
            if (!context.Request.Headers.TryGetValue("X-Forwarded-For", out var realIp))
            {
                throw new InvalidOperationException(
                    "Cannot determine IP of user. Proxy must sent X-Forwarded-For header");
            }

            return realIp.FirstOrDefault();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Get name of current user
    /// </summary>
    public static string GetUserName(this HttpContext context)
    {
        return context.User.Claims.Single(m => m.Type == ClaimTypes.Name).Value;
    }
}
