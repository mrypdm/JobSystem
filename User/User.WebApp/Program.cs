using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contract.Options;
using Shared.Database;
using User.Database.Contexts;
using User.WebApp.Extensions;
using User.WebApp.Models;

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
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/auth/login";
        opt.LogoutPath = "/auth/logout";

        opt.Events.OnValidatePrincipal = context =>
        {
            var ip = context.HttpContext.GetUserIpAddress(webServerOptions.IsProxyUsed);

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
builder.Services.AddAuthorization();

var dbOptions = builder.Configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>();
builder.Services.AddDbContext<UserDbContext>(options => UserDbContext.BuildOptions(options, dbOptions));

var certificateCache = new MemoryCache(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(15) });
var jobWebApiOptions = builder.Configuration.GetSection("JobWebApiOptions").Get<JobWebApiOptions>();
builder.Services
    .AddHttpClient("Job.WebApi", options =>
    {
        options.BaseAddress = new Uri(jobWebApiOptions.Url);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            CheckCertificateRevocationList = false, // TODO CRL
            ServerCertificateCustomValidationCallback = (message, cert, chain, policy) =>
            {
                return certificateCache.GetOrCreate(cert.Thumbprint, enty =>
                {
                    var result = jobWebApiOptions.ValidateCertificate(cert);
                    enty.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                    enty.Value = result;
                    return result;
                });
            }
        };
        handler.ClientCertificates.Add(jobWebApiOptions.Certificate);
        return handler;
    });

builder.Services.AddControllersWithViews();

builder.Services
    .AddAntiforgery(opt =>
    {
        opt.HeaderName = "X-CSRF-TOKEN";
    });

var application = builder.Build();

if (!application.Environment.IsDevelopment())
{
    application.UseExceptionHandler("/Home/Error");
}

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

application.MapStaticAssets();
application.MapControllers().WithStaticAssets();

application.Run();
