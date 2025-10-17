using System;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Job.Broker;
using Job.Broker.Options;
using Job.Database.Contexts;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contract.Options;
using Shared.Database;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(opt =>
    {
        opt.IncludeXmlComments(
            Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    });

var webServerOptions = builder.Configuration.GetSection("WebServerOptions").Get<WebServerOptions>();
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        httpsOptions.ServerCertificate = webServerOptions.Certificate;
        httpsOptions.ServerCertificateChain = webServerOptions.Chain;
    });
});

builder.Services
    .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.Chained;
        options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
        options.CustomTrustStore = webServerOptions.Chain;
        options.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        options.RevocationMode = X509RevocationMode.NoCheck; // TODO revocation list

        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                var claims = new[]
                {
                    // TODO custom fields in certificate for authorization
                    new Claim(ClaimTypes.Name, context.ClientCertificate.Subject, ClaimValueTypes.String,
                        context.ClientCertificate.Issuer)
                };

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                context.Fail("");
                return Task.CompletedTask;
            }
        };
    })
    .AddCertificateCache(options =>
    {
        options.CacheEntryExpiration = TimeSpan.FromMinutes(15);
        options.CacheSize = 100;
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var dbOptions = builder.Configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>();
builder.Services.AddDbContext<JobsDbContext>(options => JobsDbContext.BuildOptions(options, dbOptions));

var producerOptions = builder.Configuration.GetSection("ProducerOptions").Get<ProducerOptions>();
builder.Services.AddSingleton(producerOptions);
builder.Services.AddSingleton<JobProducer>();

var application = builder.Build();

application
    .UseHsts()
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization();

if (application.Environment.IsDevelopment())
{
    application
        .UseSwagger()
        .UseSwaggerUI();
    application
        .MapSwagger();
}

application.MapControllers();

application.Run();
