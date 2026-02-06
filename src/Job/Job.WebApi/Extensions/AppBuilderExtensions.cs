using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Job.Broker;
using Job.Broker.Clients;
using Job.Database.Contexts;
using Job.WebApi.Options;
using Job.WebApi.Workers;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Contract.Logging;
using Shared.Contract.Options;
using Shared.Contract.Owned;
using Shared.Database;
using Shared.WebApi.Filters;

namespace Job.WebApi.Extensions;

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
        builder.Services.AddDbContext<IJobDbContext, JobDbContext>(
            options => PostgreDbContext.BuildOptions(options, dbOptions, sslValidator),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Transient);
        builder.Services.AddSingleton<IOwnedService<IJobDbContext>, OwnedService<IJobDbContext>>();
        return builder;
    }

    /// <summary>
    /// Add lost Jobs worker
    /// </summary>
    public static WebApplicationBuilder AddLostJobsWorker(this WebApplicationBuilder builder)
    {
        var workerOptions = builder.Configuration.GetOptions<LostJobWorkerOptions>();
        builder.Services.AddSingleton(workerOptions);
        builder.Services.AddHostedService<LostJobWorker>();
        return builder;
    }

    /// <summary>
    /// Add Broker
    /// </summary>
    public static WebApplicationBuilder AddBroker(this WebApplicationBuilder builder)
    {
        var producerOptions = builder.Configuration.GetOptions<ProducerOptions>();
        builder.Services.AddSingleton(producerOptions);
        builder.Services.AddSingleton<IBrokerProducer<Guid, JobMessage>, JobProducer>();
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
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                httpsOptions.ServerCertificate = webServerOptions.Certificate;
                httpsOptions.ServerCertificateChain = webServerOptions.CertificateChain;

                // we have certificate validation as part of Authentication flow
                // so disable double check
                httpsOptions.AllowAnyClientCertificate();
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
    /// Configure controllers
    /// </summary>
    public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        var jobsControllerOptions = builder.Configuration.GetOptions<JobControllerOptions>();
        builder.Services.AddSingleton(jobsControllerOptions);
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<PostgresExceptionsFilter>();
        });
        return builder;
    }

    /// <summary>
    /// Add certificate authentication
    /// </summary>
    public static WebApplicationBuilder AddCertificateAuthentication(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            return builder;
        }

        var webServerOptions = builder.Configuration.GetOptions<WebServerOptions>();
        var sslValidator = new SslValidator(webServerOptions);
        builder.Services
            .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(certOptions =>
            {
                certOptions.AllowedCertificateTypes = CertificateTypes.Chained;
                certOptions.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
                certOptions.CustomTrustStore = webServerOptions.CertificateChain;
                certOptions.RevocationMode = X509RevocationMode.NoCheck;
                certOptions.ValidateValidityPeriod = true;
                certOptions.ValidateCertificateUse = true;
                certOptions.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        if (sslValidator.IsRevoked(context.ClientCertificate))
                        {
                            context.Fail("Client certificated is revoked");
                            return Task.CompletedTask;
                        }

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new(ClaimTypes.Name,
                                context.ClientCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false)),
                            new(ClaimTypes.X500DistinguishedName, context.ClientCertificate.Subject)
                        ], context.Scheme.Name));

                        context.Success();
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Fail(context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            })
            .AddCertificateCache(options =>
            {
                options.CacheEntryExpiration = TimeSpan.FromMinutes(15);
                options.CacheSize = 100;
            });
        builder.Services.AddAuthorization(opt => opt.FallbackPolicy = opt.DefaultPolicy);
        return builder;
    }
}
