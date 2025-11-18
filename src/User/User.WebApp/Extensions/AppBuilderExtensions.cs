using System.Reflection;
using System.Security.Claims;
using Job.WebApi.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Contract.Logging;
using Shared.Contract.Options;
using Shared.Database;
using User.Database.Contexts;

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
        builder.Services.AddSingleton(builder.Configuration.GetOptions<JobWebApiClientOptions>());
        builder.Services.AddSingleton<IJobWebApiClient, JobWebApiClient>();
        builder.Services.AddSingleton<IFlurlClientFactory, FlurlClientFactory>();
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
    /// Configure Logging
    /// </summary>
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddLogging(logBuilder =>
        {
            logBuilder.ClearProviders();
            logBuilder.AddConsoleFormatter<SimpleConsoleFormatter, SimpleFormatterOptions>();
            logBuilder.AddConsole(options => options.FormatterName = nameof(SimpleConsoleFormatter));
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
