using Microsoft.Extensions.DependencyInjection;
using Shared.Contract;
using Tests.Common;

namespace Tests.Integration;

/// <summary>
/// Base class for integration tests
/// </summary>
[NonParallelizable]
public abstract class IntegrationTestBase : TestBase
{
    [SetUp]
    public async Task InitializeServices()
    {
        await Task.WhenAll(Services.GetServices<IInitializer>().Select(m => m.InitializeAsync(default)));
    }
}
