namespace User.WebApp.Models;

/// <summary>
/// Request for user to login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Name of user
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Password of user
    /// </summary>
    public string Password { get; set; }
}
