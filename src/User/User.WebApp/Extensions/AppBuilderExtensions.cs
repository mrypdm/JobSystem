using System.Reflection;
using System.Security.Claims;
using Flurl.Http;
using Job.WebApi.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Contract.Options;
using Shared.Database;
using User.Database.Contexts;
using User.WebApp.Models;

namespace User.WebApp.Extensions;

/// <summary>
/// Extensions for <see cref="WebApplicationBuilder"/>
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Add Swagger
    /// </summary>
    public static WebApplicationBuilder AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(opt =>
            {
                opt.IncludeXmlComments(
                    Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            });
        return builder;
    }

    /// <summary>
    /// Add Database
    /// </summary>
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>();
        var sslValidator = new SslValidator(dbOptions);
        builder.Services.AddDbContext<IUserDbContext, UserDbContext>(
            options => PostgreDbContext.BuildOptions(options, dbOptions, sslValidator));
        return builder;
    }

    /// <summary>
    /// Add Job WebApi
    /// </summary>
    public static WebApplicationBuilder AddJobApi(this WebApplicationBuilder builder)
    {
        var jobWebApiOptions = builder.Configuration.GetOptions<JobWebApiOptions>();
        var sslValidator = new SslValidator(jobWebApiOptions);
        builder.Services.AddSingleton<IJobWebApiClient>(context => new JobWebApiClient(
            context.GetRequiredKeyedService<IFlurlClient>(nameof(JobWebApiClient)),
            context.GetRequiredService<ILogger<JobWebApiClient>>())
        );
        builder.Services.AddKeyedSingleton(nameof(JobWebApiClient), (_, _) =>
            FlurlHttp
                .ConfigureClientForUrl(jobWebApiOptions.Url)
                .ConfigureInnerHandler(handler =>
                {
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.CheckCertificateRevocationList = false;
                    handler.ServerCertificateCustomValidationCallback
                        = (_, cert, chain, policy) => sslValidator.Validate(cert, chain, policy);
                    handler.ClientCertificates.Add(jobWebApiOptions.Certificate);
                })
                .Build()
        );

        return builder;
    }

    /// <summary>
    /// Configure HTTPS options
    /// </summary>
    public static WebApplicationBuilder ConfigureWebServer(this WebApplicationBuilder builder)
    {
        var webServerOptions = builder.Configuration.GetOptions<WebServerOptions>();
        builder.Services.Configure<KestrelServerOptions>(kestrelOptions =>
        {
            kestrelOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB;
            kestrelOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                httpsOptions.ServerCertificate = webServerOptions.Certificate;
                httpsOptions.ServerCertificateChain = webServerOptions.CertificateChain;
            });
        });
        return builder;
    }

    /// <summary>
    /// Add certificate authentication
    /// </summary>
    public static WebApplicationBuilder AddCookieAuthentication(this WebApplicationBuilder builder)
    {
        var webServerOptions = builder.Configuration.GetOptions<WebServerOptions>();
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opt =>
            {
                opt.LoginPath = "/auth/login";
                opt.LogoutPath = "/auth/logout";

                opt.Events.OnValidatePrincipal = context =>
                {
                    var ip = context.HttpContext.GetUserIpAddress();
                    if (context.Principal.Claims
                            .SingleOrDefault(m => m.Type == HttpContextExtensions.IpAddressClaim)?.Value != ip)
                    {
                        context.RejectPrincipal();
                    }

                    if (string.IsNullOrWhiteSpace(
                            context.Principal.Claims.SingleOrDefault(m => m.Type == ClaimTypes.Name)?.Value))
                    {
                        context.RejectPrincipal();
                    }

                    return Task.CompletedTask;
                };
            });
        return builder;
    }
}
