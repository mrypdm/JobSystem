using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tests.Integration.Initializers;

namespace Tests.Integration;

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    private readonly IHost _host;

    protected IntegrationTestBase()
    {
        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
        builder.Services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddNUnit();
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        ConfigureServices(builder.Services);
        _host = builder.Build();
    }

    /// <summary>
    /// Registered services
    /// </summary>
    protected IServiceProvider Services => _host.Services;

    public void Dispose()
    {
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    [OneTimeSetUp]
    public async Task InitializeServices(CancellationToken cancellationToken)
    {
        var initializers = _host.Services.GetServices<IInitializer>();
        foreach (var initializer in initializers)
        {
            await initializer.InitializeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Configure services for tests
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
