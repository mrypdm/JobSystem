using Microsoft.Extensions.DependencyInjection;
using Tests.Common.Initializers;

namespace Tests.Common;

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : TestBase
{
    [SetUp]
    public async Task InitializeServices()
    {
        // GetServices uses GetRequiredService, which throws an exception if no services have been registered
        var initializers = Services.GetService<IEnumerable<IInitializer>>() ?? [];
        foreach (var initializer in initializers)
        {
            await initializer.InitializeAsync(default);
        }
    }
}
