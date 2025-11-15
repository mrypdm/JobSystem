using Job.Broker.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;
using Shared.Contract.Extensions;
using Tests.Common;
using Tests.Common.Initializers;

namespace Job.Broker.Tests;

/// <summary>
/// Tests for <see cref="JobProducer"/> and <see cref="JobConsumer"/>
/// </summary>
[NonParallelizable]
internal class BrokerTests : IntegrationTestBase
{
    [Test]
    [Retry(3)] // Sometimes Kafka doesn't have time to load the ACL, causing the test to fail with authorization error
    public async Task ProduceConsume_ShouldSendMessage_ShouldReadMessage()
    {
        // arrange
        var expectedMessage = new JobMessage() { Id = Guid.NewGuid() };

        var producer = Services.GetRequiredService<IJobProducer<Guid, JobMessage>>();
        var consumer = Services.GetRequiredService<IJobConsumer<Guid, JobMessage>>();
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

        builder.Services.AddSingleton<IBrokerAdminClient, BrokerAdminClient>();
        builder.Services.AddScoped<IJobProducer<Guid, JobMessage>, JobProducer>();
        builder.Services.AddScoped<IJobConsumer<Guid, JobMessage>, JobConsumer>();

        builder.Services.AddScoped<IInitializer>(
            context => new BrokerInitializer(context.GetRequiredService<IBrokerAdminClient>()));
    }
}
