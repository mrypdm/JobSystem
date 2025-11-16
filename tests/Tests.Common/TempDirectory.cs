namespace Tests.Common;

/// <summary>
/// Temporary directory
/// </summary>
public sealed class TempDirectory : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory();

    /// <summary>
    /// Path to directory
    /// </summary>
    public string Path => directory.FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            directory.Delete(recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // NOP
        }
    }
}
