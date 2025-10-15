using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Job.WebApi;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var serverCert = builder.Configuration.GetSection("Certificates:WebServer").Get<CertificateOptions>();

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        httpsOptions.ServerCertificate = serverCert.Certificate;
        httpsOptions.ServerCertificateChain = serverCert.Chain;
    });
});

builder.Services
    .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.Chained;
        options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
        options.CustomTrustStore = serverCert.Chain;
        options.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        options.RevocationMode = X509RevocationMode.NoCheck; // TODO revocation list

        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                var claims = new[]
                {
                    // TODO custom fields in certificate for authorization
                    new Claim(ClaimTypes.NameIdentifier, context.ClientCertificate.Subject,
                        ClaimValueTypes.String, context.ClientCertificate.Issuer),
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

var app = builder.Build();

app
    .UseHsts()
    .UseHttpsRedirection()
    .UseAuthentication()
    .UseAuthorization()
    .UseRouting();

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
