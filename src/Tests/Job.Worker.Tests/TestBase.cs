using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Tests;

/// <summary>
/// Base class for tests
/// </summary>
[TestFixture]
internal abstract class TestBase
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole().AddNUnit();
        builder.SetMinimumLevel(LogLevel.Trace);
    });
    private readonly DirectoryInfo _tempDir = Directory.CreateTempSubdirectory();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _tempDir.Delete(recursive: true);
        _loggerFactory.Dispose();
    }

    /// <summary>
    /// Creates temporary directroy for test
    /// </summary>
    protected string CreateTempDir([CallerMemberName] string testName = null)
    {
        return _tempDir.CreateSubdirectory(testName).FullName;
    }

    /// <summary>
    /// Create logger for <typeparamref name="TClass"/>
    /// </summary>
    protected ILogger<TClass> CreateLogger<TClass>()
    {
        return new Logger<TClass>(_loggerFactory);
    }
}
