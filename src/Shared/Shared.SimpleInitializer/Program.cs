using Job.Broker.Clients;
using Job.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Shared.Broker.Abstractions;
using Shared.Broker.Helpers;
using Shared.Broker.Options;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;
using Shared.Database.Migrations;
using User.Database.Contexts;
using ILogger = Serilog.ILogger;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(
    (_, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(builder.Configuration));

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
    return new JobDbContext(options.Options, context.GetRequiredService<ILogger>());
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
    return new UserDbContext(options.Options, context.GetRequiredService<ILogger>());
});
builder.Services.AddTransient<IInitializer>(
    context => new DbInitializer(context.GetRequiredService<UserDbContext>()));

using var host = builder.Build();

var lifeTime = host.Services.GetService<IHostApplicationLifetime>()
    ?? throw new InvalidOperationException("Cannot get application lifetime");
await Task.WhenAll(host.Services.GetServices<IInitializer>()
    .Select(m => m.InitializeAsync(lifeTime.ApplicationStopping)));
