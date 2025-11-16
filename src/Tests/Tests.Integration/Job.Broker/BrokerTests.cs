using Job.Broker;
using Job.Broker.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;
using Shared.Contract.Extensions;
using Tests.Integration.Initializers;

namespace Tests.Integration.Job.Broker;

/// <summary>
/// Tests for <see cref="JobProducer"/> and <see cref="JobConsumer"/>
/// </summary>
internal class BrokerTests : IntegrationTestBase
{
    [Test]
    public async Task ProduceConsume_ShouldSendMessage_ShouldReadMessage()
    {
        // arrange
        var expectedMessage = new JobMessage() { Id = Guid.NewGuid() };

        using var producer = Services.GetRequiredService<IJobProducer<Guid, JobMessage>>();
        using var consumer = Services.GetRequiredService<IJobConsumer<Guid, JobMessage>>();
        consumer.Subscribe();

        // act
        await producer.PublishAsync(expectedMessage, default);
        var consumeResult = consumer.Consume(default);
        consumer.Commit(consumeResult);

        // assert

        using var _ = Assert.EnterMultipleScope();
        Assert.That(consumeResult.Message.Key, Is.EqualTo(expectedMessage.Id));
        Assert.That(consumeResult.Message.Value.Id, Is.EqualTo(expectedMessage.Id));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        builder.Services.AddSingleton(builder.Configuration.GetOptions<AdminOptions>());
        builder.Services.AddSingleton(builder.Configuration.GetOptions<ProducerOptions>());
        builder.Services.AddSingleton(builder.Configuration.GetOptions<ConsumerOptions>());

        builder.Services.AddTransient<IBrokerAdminClient, BrokerAdminClient>();
        builder.Services.AddTransient<IJobProducer<Guid, JobMessage>, JobProducer>();
        builder.Services.AddTransient<IJobConsumer<Guid, JobMessage>, JobConsumer>();

        builder.Services.AddTransient<IInitializer>(
            context => new BrokerInitializer(context.GetRequiredService<IBrokerAdminClient>()));
    }
}
