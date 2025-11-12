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
        ConfigureServices(builder.Services);
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
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddNUnit();
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        services.AddTransient<TempDirectory>();
    }
}
