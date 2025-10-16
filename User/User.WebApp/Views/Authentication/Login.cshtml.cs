namespace User.WebApp.Views.Authentication;

/// <summary>
/// Model for login view
/// </summary>
public class LoginModel(string returnUrl)
{
    /// <summary>
    /// URL for redirect after login
    /// </summary>
    public string ReturnUrl { get; } = returnUrl ?? "/";
}
