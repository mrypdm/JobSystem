using Microsoft.Extensions.DependencyInjection;
using Tests.Unit.Initializers;

namespace Tests.Unit;

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : UnitTestBase
{
    [OneTimeSetUp]
    public async Task InitializeServices(CancellationToken cancellationToken)
    {
        // GetServices uses GetRequiredService, which throws an exception if no services have been registered
        var initializers = Services.GetService<IEnumerable<IInitializer>>() ?? [];
        foreach (var initializer in initializers)
        {
            await initializer.InitializeAsync(cancellationToken);
        }
    }
}
