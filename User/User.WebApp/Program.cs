using Microsoft.AspNetCore.HttpOverrides;
using User.WebApp.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder
    .AddSwagger()
    .ConfigureHttps()
    .AddDatabase()
    .AddJobApi()
    .AddCookieAuthentication();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(opt =>
{
    opt.HeaderName = "X-CSRF-TOKEN";
});

var application = builder.Build();

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
