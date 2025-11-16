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
        return FlurlHttp
            .ConfigureClientForUrl(options.Url)
            .ConfigureInnerHandler(handler =>
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.CheckCertificateRevocationList = false;
                handler.ServerCertificateCustomValidationCallback
                    = (_, cert, _, policy) => sslValidator.Validate(cert, policy);
                handler.ClientCertificates.Add(options.Certificate);
            })
            .Build();
    }
}
