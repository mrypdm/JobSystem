using Job.Broker;
using Job.Broker.Clients;
using Job.Database.Contexts;
using Job.Worker.Collectors;
using Job.Worker.Environments;
using Job.Worker.JobProcesses;
using Job.Worker.Options;
using Job.Worker.Processes;
using Job.Worker.Resources.Analyzers;
using Job.Worker.Resources.Readers;
using Job.Worker.Runners;
using Job.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
});

var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>();
var sslValidator = new SslValidator(dbOptions);
builder.Services.AddDbContext<IJobDbContext, JobDbContext>(
    options => PostgreDbContext.BuildOptions(options, dbOptions, sslValidator));

builder.Services.AddSingleton(builder.Configuration.GetOptions<ConsumerOptions>());
builder.Services.AddSingleton<IJobConsumer<Guid, JobMessage>, JobConsumer>();

builder.Services.AddSingleton(builder.Configuration.GetOptions<JobEnvironmentOptions>());
builder.Services.AddSingleton<IJobEnvironment, LinuxDockerJobEnvironment>();

builder.Services.AddSingleton(builder.Configuration.GetOptions<ResourcesAnalyzerOptions>());
builder.Services.AddSingleton<IResourcesAnalyzer, ResourcesAnalyzer>();

builder.Services.AddSingleton<IResourcesReader, LinuxResourcesReader>();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IResultsCollector, ZipResultsCollector>();
builder.Services.AddSingleton<IJobProcessRunner, DockerJobProcessRunner>();

builder.Services.AddSingleton<IJobRunner, JobRunner>();

builder.Services.AddSingleton(builder.Configuration.GetOptions<ConsumerWorkerOptions>());
builder.Services.AddHostedService<ConsumerWorker>();

var app = builder.Build();
app.Run();
