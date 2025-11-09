using Job.Broker;
using Job.Broker.Consumers;
using Job.Broker.Options;
using Job.Database.Contexts;
using Job.Worker.Collectors;
using Job.Worker.Environments;
using Job.Worker.Monitors;
using Job.Worker.Options;
using Job.Worker.Processes;
using Job.Worker.Runners;
using Job.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;

var builder = Host.CreateApplicationBuilder(args);

var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>();
var sslValidator = new SslValidator(dbOptions);
builder.Services.AddDbContext<IJobDbContext, JobDbContext>(
    options => PostgreDbContext.BuildOptions(options, dbOptions, sslValidator));

builder.Services.AddSingleton(builder.Configuration.GetOptions<ConsumerOptions>());
builder.Services.AddSingleton<IJobConsumer<Guid, JobMessage>, JobConsumer>();

builder.Services.AddSingleton(builder.Configuration.GetOptions<JobEnvironmentOptions>());
builder.Services.AddSingleton<IJobEnvironment, LinuxDockerJobEnvironment>();

builder.Services.AddSingleton(builder.Configuration.GetOptions<ResourceMonitorOptions>());
builder.Services.AddSingleton<IResourceMonitor, LinuxResourceMonitor>();

builder.Services.AddSingleton<IResultsCollector, ZipResultsCollector>();
builder.Services.AddSingleton<IJobProcessRunner, DockerProcessRunner>();

builder.Services.AddSingleton<IJobRunner, JobRunner>();

builder.Services.AddSingleton(builder.Configuration.GetOptions<ConsumerWorkerOptions>());
builder.Services.AddHostedService<ConsumerWorker>();

var app = builder.Build();
app.Run();
