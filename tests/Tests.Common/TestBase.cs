using Microsoft.Extensions.Hosting;
using Serilog;

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
        builder.Services.AddSerilog(
            (_, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(builder.Configuration));
    }
}
