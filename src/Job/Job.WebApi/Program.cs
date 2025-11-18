using Job.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .ConfigureLogging()
    .ConfigureWebServer()
    .AddSwagger()
    .AddCertificateAuthentication()
    .AddDatabase()
    .AddLostJobsWorker()
    .AddBroker()
    .AddControllers();

using var application = builder.Build();

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
