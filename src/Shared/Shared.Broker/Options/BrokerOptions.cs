using System.Net;
using System.Reflection;
using Shared.Contract.SslOptions;

namespace Shared.Broker.Options;

/// <summary>
/// Common options for Broker
/// </summary>
public abstract class BrokerOptions<TConfig> : PemCertificateOptions
{
    /// <summary>
    /// Broker servers in format 'host:port'
    /// </summary>
    public string Servers { get; set; }

    /// <summary>
    /// Client Id
    /// </summary>
    public string ClientId { get; } = $"{Assembly.GetEntryAssembly()?.GetName().Name}-{Dns.GetHostName()}";

    /// <summary>
    /// Name of topic
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// Convert options to <typeparamref name="TConfig"/>
    /// </summary>
    public abstract TConfig ToConfig();
}
