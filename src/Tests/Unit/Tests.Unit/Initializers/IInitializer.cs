namespace Tests.Unit.Initializers;

/// <summary>
/// Initializer for something
/// </summary>
internal interface IInitializer
{
    /// <summary>
    /// Initialize something
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken);
}
