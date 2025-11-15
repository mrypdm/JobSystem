using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tests.Common;

/// <summary>
/// Base class for tests
/// </summary>
[TestFixture]
public abstract class TestBase
{
    private IHost _host;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
        ConfigureServices(builder);
        _host = builder.Build();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _host.Dispose();
    }

    // <summary>
    /// Registered services
    /// </summary>
    protected IServiceProvider Services => _host.Services;

    /// <summary>
    /// Configure services for tests
    /// </summary>
    protected virtual void ConfigureServices(HostApplicationBuilder builder)
    {
        builder.Services.AddLogging(logBuilder =>
        {
            logBuilder.AddConsole();
            logBuilder.AddNUnit();
            logBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        builder.Services.AddTransient<TempDirectory>();
    }
}
