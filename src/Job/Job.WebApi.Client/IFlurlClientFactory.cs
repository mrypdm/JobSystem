using Flurl.Http;

namespace Job.WebApi.Client;

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
