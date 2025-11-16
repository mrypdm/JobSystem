using System.Net;
using Job.WebApi.Extensions;

var envVars = Environment.GetEnvironmentVariables();
foreach (var env in envVars.Keys)
{
    Console.WriteLine($"{env} = {envVars[env]}");
}

if (envVars.Contains("ProducerOptions__Servers"))
{
    var server = (string)envVars["ProducerOptions__Servers"];

    if (server != "kafka:8500")
    {
        throw new Exception($"invalid server: {server}");
    }

    var serverIps = Dns.GetHostAddresses("kafka");
    foreach (var ip in serverIps)
    {
        Console.WriteLine(ip);
    }
}
else
{
    throw new Exception("there is no kafka servers");
}

return;

var builder = WebApplication.CreateBuilder(args);

builder
    .ConfigureWebServer()
    .AddSwagger()
    .AddCertificateAuthentication()
    .AddDatabase()
    .AddLostJobsWorker()
    .AddBroker()
    .AddControllers();

var application = builder.Build();

application
    .UseHsts()
    .UseHttpsRedirection()
    .UseRouting();

if (!application.Environment.IsDevelopment())
{
    application
        .UseAuthentication()
        .UseAuthorization();
}

if (application.Environment.IsDevelopment())
{
    application
        .UseSwagger()
        .UseSwaggerUI();
    application
        .MapSwagger();
}

application.MapControllers();
application.Run();
