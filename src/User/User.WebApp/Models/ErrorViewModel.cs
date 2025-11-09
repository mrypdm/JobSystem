namespace User.WebApp.Models;

/// <summary>
/// Model for errors
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Id of request
    /// </summary>
    public string RequestId { get; set; }

    /// <summary>
    /// Is request Id shown
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
