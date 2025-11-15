namespace Tests.Common.Initializers;

/// <summary>
/// Initializer for something
/// </summary>
public interface IInitializer
{
    /// <summary>
    /// Initialize something
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken);
}
