namespace User.WebApp.Models;

/// <summary>
/// Request for user to login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Where user should be redirected after login
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    /// Name of user
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Password of user
    /// </summary>
    public string Password { get; set; }
}
