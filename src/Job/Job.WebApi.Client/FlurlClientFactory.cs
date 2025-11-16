using System.Security.Authentication;
using Flurl.Http;
using Shared.Contract;

namespace Job.WebApi.Client;

/// <inheritdoc />
public class FlurlClientFactory : IFlurlClientFactory
{
    /// <inheritdoc />
    public IFlurlClient Create(JobWebApiClientOptions options)
    {
        var sslValidator = new SslValidator(options);
        var client = FlurlHttp
            .ConfigureClientForUrl(options.Url)
            .ConfigureInnerHandler(handler =>
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.CheckCertificateRevocationList = false;
                handler.SslProtocols = SslProtocols.Tls13;
                handler.ServerCertificateCustomValidationCallback
                    = (_, cert, chain, policy) => sslValidator.Validate(cert, chain, policy);
                handler.ClientCertificates.Add(options.Certificate);
            })
            .Build();
        client.BaseUrl = options.Url;
        return client;
    }
}
