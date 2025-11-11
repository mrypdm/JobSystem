using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Tests.Common;

/// <summary>
/// Base class for unit tests
/// </summary>
[TestFixture]
public abstract class UnitTestBase(bool withTempDir = false)
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole().AddNUnit();
        builder.SetMinimumLevel(LogLevel.Trace);
    });
    private readonly DirectoryInfo _tempDir = withTempDir ? Directory.CreateTempSubdirectory() : null;

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _tempDir?.Delete(recursive: true);
        _loggerFactory.Dispose();
    }

    /// <summary>
    /// Creates temporary directroy for test
    /// </summary>
    protected string CreateTempDir([CallerMemberName] string testName = null)
    {
        return _tempDir?.CreateSubdirectory(testName).FullName
            ?? throw new InvalidOperationException("Tests are not configured for using temporary directory");
    }

    /// <summary>
    /// Create logger for <typeparamref name="TClass"/>
    /// </summary>
    protected ILogger<TClass> CreateLogger<TClass>()
    {
        return new Logger<TClass>(_loggerFactory);
    }
}
