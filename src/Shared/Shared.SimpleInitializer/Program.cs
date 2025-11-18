using Job.Broker.Clients;
using Job.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Helpers;
using Shared.Broker.Options;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Contract.Logging;
using Shared.Contract.Options;
using Shared.Database;
using Shared.Database.Migrations;
using User.Database.Contexts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(logBuilder =>
{
    logBuilder.ClearProviders();
    logBuilder.AddConsoleFormatter<SimpleConsoleFormatter, SimpleFormatterOptions>();
    logBuilder.AddConsole(options => options.FormatterName = nameof(SimpleConsoleFormatter));
});

builder.Services.AddSingleton(builder.Configuration.GetOptions<AdminOptions>());
builder.Services.AddTransient<IBrokerAdminClient, BrokerAdminClient>();
builder.Services.AddTransient<IInitializer>(
    context => new BrokerInitializer(context.GetRequiredService<IBrokerAdminClient>()));

var jobDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("AdminJobsDatabaseOptions");
var jobDbSslValidator = new SslValidator(jobDbOptions);
builder.Services.AddTransient(context =>
{
    var options = PostgreDbContext.BuildOptions(
        new DbContextOptionsBuilder(),
        jobDbOptions,
        jobDbSslValidator,
        context.GetRequiredService<ILoggerFactory>());
    return new JobDbContext(options.Options, context.GetRequiredService<ILogger<JobDbContext>>());
});
builder.Services.AddTransient<IInitializer>(
    context => new DbInitializer(context.GetRequiredService<JobDbContext>()));

var userDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("AdminUsersDatabaseOptions");
var userDbSslValidator = new SslValidator(userDbOptions);
builder.Services.AddTransient(context =>
{
    var options = PostgreDbContext.BuildOptions(
        new DbContextOptionsBuilder(),
        userDbOptions,
        userDbSslValidator,
        context.GetRequiredService<ILoggerFactory>());
    return new UserDbContext(options.Options, context.GetRequiredService<ILogger<UserDbContext>>());
});
builder.Services.AddTransient<IInitializer>(
    context => new DbInitializer(context.GetRequiredService<UserDbContext>()));

using var host = builder.Build();

var lifeTime = host.Services.GetService<IHostApplicationLifetime>()
    ?? throw new InvalidOperationException("Cannot get application lifetime");
await Task.WhenAll(host.Services.GetServices<IInitializer>()
    .Select(m => m.InitializeAsync(lifeTime.ApplicationStopping)));
