using Microsoft.EntityFrameworkCore;

namespace User.Database.Models;

/// <summary>
/// Model of user
/// </summary>
[PrimaryKey(nameof(Username))]
public class UserDbModel
{
    /// <summary>
    /// Name of user
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Hash of password of user
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// Salt of password of user
    /// </summary>
    public string PasswordSalt { get; set; }
}
