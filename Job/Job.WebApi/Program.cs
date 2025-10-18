using Job.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSwagger()
    .ConfigureHttps()
    .AddCertificateAuthentication()
    .AddDatabase()
    .AddBroker();
builder.Services.AddControllers();

var application = builder.Build();

application
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

application.MapControllers();
application.Run();
