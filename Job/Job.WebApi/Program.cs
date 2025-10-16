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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contract.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

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
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var dbOptions = builder.Configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>();
builder.Services.AddDbContext<JobsDbContext>(options => options.UseNpgsql(JobsDbContext.GetConnectionString(dbOptions)));

var producerOptions = builder.Configuration.GetSection("ProducerOptions").Get<ProducerOptions>();
builder.Services.AddSingleton(producerOptions);
builder.Services.AddSingleton<JobProducer>();

var app = builder.Build();

app
    .UseHsts()
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
