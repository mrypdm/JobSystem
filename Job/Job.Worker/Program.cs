using Job.Broker.Consumers;
using Job.Broker.Options;
using Job.Database.Contexts;
using Job.Worker.Options;
using Job.Worker.Services;
using Job.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contract.Extensions;
using Shared.Database;

var builder = Host.CreateApplicationBuilder(args);

var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>();
builder.Services.AddDbContext<IJobDbContext, JobDbContext>(options => JobDbContext.BuildOptions(options, dbOptions));

var consumerOptions = builder.Configuration.GetOptions<ConsumerOptions>();
builder.Services.AddSingleton(consumerOptions);
builder.Services.AddSingleton<IJobConsumer, JobConsumer>();

var jobRunnerOptions = builder.Configuration.GetOptions<JobRunnerOptions>();
builder.Services.AddSingleton(jobRunnerOptions);
builder.Services.AddSingleton<JobRunner>();

var consumerWorkerOptions = builder.Configuration.GetOptions<ConsumerWorkerOptions>();
builder.Services.AddSingleton(consumerWorkerOptions);
builder.Services.AddHostedService<ConsumerWorker>();

var app = builder.Build();
app.Run();
