using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contract.Extensions;

namespace Tests.Common;

/// <summary>
/// Base class for tests
/// </summary>
[TestFixture]
public abstract class TestBase
{
    private IHost _host;

    [OneTimeSetUp]
    public void ConfigureTestHost()
    {
        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
        ConfigureServices(builder);
        _host = builder.Build();
    }

    [OneTimeTearDown]
    public void DisposeTestHost()
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
            logBuilder.ClearProviders();
            logBuilder
                .SetMinimumLevel(LogLevel.Information)
                .AddFilter(null, LogLevel.Information);
            logBuilder.AddSimpleLogger(
                logOptions => logOptions.IsEnabled = () => TestContext.Progress is not null,
                formatOptions => formatOptions.WithColors = false);
        });
    }
}
