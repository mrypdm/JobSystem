using Microsoft.Extensions.DependencyInjection;

namespace Shared.Contract.Owned;

/// <inheritdoc />
public class OwnedService<TService>(IServiceProvider serviceProvider) : IOwnedService<TService>
{
    /// <inheritdoc />
    public TService Value => serviceProvider.GetRequiredService<TService>();
}
