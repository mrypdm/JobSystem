using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Caching.Memory;
using Shared.Contract;
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
        httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
        httpsOptions.ServerCertificate = webServerOptions.Certificate;
        httpsOptions.ServerCertificateChain = webServerOptions.CertificateChain;
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
builder.Services.AddAuthorization(opt => opt.FallbackPolicy = opt.DefaultPolicy);

var dbOptions = builder.Configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>();
builder.Services.AddDbContext<UserDbContext>(options => UserDbContext.BuildOptions(options, dbOptions));

var jobWebApiOptions = builder.Configuration.GetSection("JobWebApiOptions").Get<JobWebApiOptions>();
var sslValidator = new SslValidator(jobWebApiOptions);
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
            CheckCertificateRevocationList = false,
            ServerCertificateCustomValidationCallback
                = (_, cert, chain, policy) => sslValidator.Validate(cert, chain, policy)
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
