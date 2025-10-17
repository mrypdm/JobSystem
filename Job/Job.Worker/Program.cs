using Job.Broker;
using Job.Broker.Options;
using Job.Database.Contexts;
using Job.Worker.Options;
using Job.Worker.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contract.Options;

var builder = Host.CreateApplicationBuilder(args);

var dbOptions = builder.Configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>();
builder.Services.AddDbContext<JobsDbContext>(options => options.UseNpgsql(JobsDbContext.GetConnectionString(dbOptions)));

var consumerOptions = builder.Configuration.GetSection("ConsumerOptions").Get<ConsumerOptions>();
builder.Services.AddSingleton(consumerOptions);
builder.Services.AddSingleton<JobConsumer>();

var jobRunnerOptions = builder.Configuration.GetSection("JobRunnerOptions").Get<JobRunnerOptions>();
builder.Services.AddSingleton(jobRunnerOptions);
builder.Services.AddSingleton<JobRunner>();

var consumerWorkerOptions = builder.Configuration.GetSection("ConsumerWorkerOptions").Get<ConsumerWorkerOptions>();
builder.Services.AddSingleton(consumerWorkerOptions);
builder.Services.AddHostedService<ConsumerWorker>();

var app = builder.Build();
app.Run();
