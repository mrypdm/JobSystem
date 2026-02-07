using System.Reflection;
using System.Security.Claims;
using Job.WebApi.Client.Clients;
using Job.WebApi.Client.Factories;
using Job.WebApi.Client.Options;
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
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024; // 100 MB;
            options.ExpirationScanFrequency = TimeSpan.FromHours(1);
        });

        var webServerOptions = builder.Configuration.GetOptions<WebServerOptions>();
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opt =>
            {
                opt.LoginPath = "/auth/login";
                opt.LogoutPath = "/auth/logout";

                opt.Events.OnValidatePrincipal = context =>
                {
                    ArgumentNullException.ThrowIfNull(context);

                    if (context.Principal is null
                        || context.Principal.Claims is null
                        || context.Principal.Claims.Any(m => m is null || m.Type is null))
                    {
                        context.RejectPrincipal();
                        return Task.CompletedTask;
                    }

                    var ip = context.HttpContext.GetUserIpAddress();
                    var claims = context.Principal.Claims;

                    if (claims.SingleOrDefault(m => m.Type == HttpContextExtensions.IpAddressClaim)?.Value != ip
                        || string.IsNullOrWhiteSpace(claims.SingleOrDefault(m => m.Type == ClaimTypes.Name)?.Value))
                    {
                        context.RejectPrincipal();
                    }

                    return Task.CompletedTask;
                };
            });

        return builder;
    }
}
