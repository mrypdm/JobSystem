using Flurl.Http;
using Job.WebApi.Client.Options;

namespace Job.WebApi.Client.Factories;

/// <summary>
/// Factory for <see cref="IFlurlClient"/>
/// </summary>
public interface IFlurlClientFactory
{
    /// <summary>
    /// Create client
    /// </summary>
    IFlurlClient Create(JobWebApiClientOptions options);
}
