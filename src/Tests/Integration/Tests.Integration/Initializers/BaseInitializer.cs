namespace Tests.Integration.Initializers;

/// <summary>
/// Initializer for integration tests
/// </summary>
internal abstract class BaseInitializer : IInitializer
{
    private static bool _isInitialized = false;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _isInitialized, true, false) != false)
        {
            return;
        }

        await InitializeInternalAsync(cancellationToken);
    }

    /// <summary>
    /// Do initialization logic here. This method is called only once per application lifetime.
    /// </summary>
    protected abstract Task InitializeInternalAsync(CancellationToken cancellationToken);
}
