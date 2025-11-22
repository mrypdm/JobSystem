using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using User.WebApp.Extensions;
using User.WebApp.Filters;

var builder = WebApplication.CreateBuilder(args);
builder
    .ConfigureLogging()
    .ConfigureWebServer()
    .AddSwagger()
    .AddDatabase()
    .AddJobApi()
    .AddCookieAuthentication();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add<PostgresExceptionsFilter>();
    opt.Filters.Add<JobWebApiExceptionsFilter>();
    opt.Filters.Add<JobWebApiTimeoutExceptionFilter>();
});
builder.Services.AddAntiforgery(opt =>
{
    opt.HeaderName = "X-CSRF-TOKEN";
});

if (!builder.Environment.IsProduction())
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

using var application = builder.Build();

if (!application.Environment.IsDevelopment())
{
    application.UseExceptionHandler("/Home/Error");
}

application
    .UseForwardedHeaders()
    .UseHsts()
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization();

if (!application.Environment.IsProduction())
{
    application
        .UseSwagger()
        .UseSwaggerUI();
    application
        .MapSwagger();
}

application.MapControllers().WithStaticAssets();
application.MapStaticAssets();
application.Run();
