using Confluent.Kafka;
using Job.Broker;
using Job.Broker.Consumers;
using Job.Contract;
using Job.Database.Contexts;
using Job.Worker.Models;
using Job.Worker.Options;
using Job.Worker.Resources.Analyzers;
using Job.Worker.Runners;
using Job.Worker.Workers;
using Moq;
using Npgsql;
using Tests.Common;

namespace Job.Worker.Tests;

/// <summary>
/// Tests for <see cref="ConsumerWorker"/>
/// </summary>
[TestFixture]
internal class ConsumerWorkerTests : UnitTestBase
{
    private readonly Mock<IJobConsumer<Guid, JobMessage>> _consumer = new();
    private readonly Mock<IJobRunner> _runner = new();
    private readonly Mock<IResourcesAnalyzer> _resourceMonitor = new();
    private readonly Mock<IJobDbContext> _jobDbContext = new();
    private readonly ConsumerWorkerOptions _consumerWorkerOptions = new()
    {
        IterationDeplay = TimeSpan.FromSeconds(1)
    };

    [SetUp]
    public void SetUp()
    {
        _consumer.Reset();
        _runner.Reset();
        _jobDbContext.Reset();
    }

    [Test]
    public async Task ConsumeOnce_FailedToConsume_NoThrow()
    {
        // arrange
        _consumer
            .Setup(m => m.Consume(It.IsAny<CancellationToken>()))
            .Throws(new Exception());

        var worker = CreateWorker();

        // act
        await worker.ConsumeOnceAsync(default);

        // Assert
        Assert.That(_jobDbContext.Invocations, Has.Count.Zero);
        Assert.That(_runner.Invocations, Has.Count.Zero);
        _consumer.Verify(m => m.Commit(It.IsAny<ConsumeResult<Guid, JobMessage>>()), Times.Never);
    }

    [Test]
    public async Task ConsumeOnce_FailedToGetJobFromDb_OnSecondIteration_ShouldNotConsume()
    {
        // arrange
        var jobId = Guid.NewGuid();
        var consumeResult = new ConsumeResult<Guid, JobMessage>()
        {
            Message = new Message<Guid, JobMessage>()
            {
                Key = jobId,
                Value = new JobMessage() { Id = jobId }
            }
        };
        var jobModel = new NewJobModel
        {
            Id = jobId,
            Timeout = TimeSpan.FromSeconds(2),
            Script = "script"
        };

        _consumer
            .Setup(m => m.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);
        _jobDbContext
            .SetupSequence(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PostgresException("Very bad exception", "", "", ""))
            .ReturnsAsync(jobModel);

        var worker = CreateWorker();

        // act
        await worker.ConsumeOnceAsync(default);
        await worker.ConsumeOnceAsync(default);

        // Assert
        _consumer.Verify(m => m.Consume(It.IsAny<CancellationToken>()), Times.Once);
        _consumer.Verify(m => m.Commit(consumeResult), Times.Once);
        _jobDbContext.Verify(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _jobDbContext.Verify(m => m.SetJobRunningAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
        _runner.Verify(m => m.RunJob(It.Is<RunJobModel>(m => m.Id == jobId)), Times.Once);
    }

    [Test]
    public async Task ConsumeOnce_FailedToRunJob_OnSecondIteration_ShouldNotConsume()
    {
        // arrange
        var jobId = Guid.NewGuid();
        var consumeResult = new ConsumeResult<Guid, JobMessage>()
        {
            Message = new Message<Guid, JobMessage>()
            {
                Key = jobId,
                Value = new JobMessage() { Id = jobId }
            }
        };
        var jobModel = new NewJobModel
        {
            Id = jobId,
            Timeout = TimeSpan.FromSeconds(2),
            Script = "script"
        };

        _consumer
            .Setup(m => m.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);
        _jobDbContext
            .Setup(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobModel);
        _runner
            .SetupSequence(m => m.RunJob(It.IsAny<RunJobModel>()))
            .Throws(new Exception("Very bad exception"))
            .Pass();

        var worker = CreateWorker();

        // act
        await worker.ConsumeOnceAsync(default);
        await worker.ConsumeOnceAsync(default);

        // Assert
        _consumer.Verify(m => m.Consume(It.IsAny<CancellationToken>()), Times.Once);
        _consumer.Verify(m => m.Commit(consumeResult), Times.Once);
        _jobDbContext.Verify(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _jobDbContext.Verify(m => m.SetJobRunningAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
        _runner.Verify(m => m.RunJob(It.Is<RunJobModel>(m => m.Id == jobId)), Times.Exactly(2));
    }

    [Test]
    public async Task ConsumeOnce_FailedToSetRunning_OnSecondIteration_ShouldNotConsume()
    {
        // arrange
        var jobId = Guid.NewGuid();
        var consumeResult = new ConsumeResult<Guid, JobMessage>()
        {
            Message = new Message<Guid, JobMessage>()
            {
                Key = jobId,
                Value = new JobMessage() { Id = jobId }
            }
        };
        var jobModel = new NewJobModel
        {
            Id = jobId,
            Timeout = TimeSpan.FromSeconds(2),
            Script = "script"
        };

        _consumer
            .Setup(m => m.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);
        _jobDbContext
            .Setup(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobModel);
        _jobDbContext
            .SetupSequence(m => m.SetJobRunningAsync(jobId, It.IsAny<CancellationToken>()))
            .Throws(new PostgresException("Very bad exception", "", "", ""))
            .PassAsync();

        var worker = CreateWorker();

        // act
        await worker.ConsumeOnceAsync(default);
        await worker.ConsumeOnceAsync(default);

        // Assert
        _consumer.Verify(m => m.Consume(It.IsAny<CancellationToken>()), Times.Once);
        _consumer.Verify(m => m.Commit(consumeResult), Times.Once);
        _jobDbContext.Verify(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _jobDbContext.Verify(m => m.SetJobRunningAsync(jobId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _runner.Verify(m => m.RunJob(It.Is<RunJobModel>(m => m.Id == jobId)), Times.Exactly(2));
    }

    [Test]
    public async Task ConsumeOnce_DbReturnedNull_ShouldNotRun_And_OnSecondIteration_ShouldConsumeNext()
    {
        // arrange
        var jobId = Guid.NewGuid();
        var consumeResult = new ConsumeResult<Guid, JobMessage>()
        {
            Message = new Message<Guid, JobMessage>()
            {
                Key = jobId,
                Value = new JobMessage() { Id = jobId }
            }
        };

        _consumer
            .Setup(m => m.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);
        _jobDbContext
            .Setup(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewJobModel)null);

        var worker = CreateWorker();

        // act
        await worker.ConsumeOnceAsync(default);
        await worker.ConsumeOnceAsync(default);

        // Assert
        _consumer.Verify(m => m.Consume(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _consumer.Verify(m => m.Commit(consumeResult), Times.Exactly(2));
        _jobDbContext.Verify(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _jobDbContext.Verify(m => m.SetJobRunningAsync(jobId, It.IsAny<CancellationToken>()), Times.Never);
        _runner.Verify(m => m.RunJob(It.Is<RunJobModel>(m => m.Id == jobId)), Times.Never);
    }

    [Test]
    public async Task ConsumingLoop_ShouldUseCorrectOrder()
    {
        // arrange
        var jobId = Guid.NewGuid();
        var consumeResult = new ConsumeResult<Guid, JobMessage>()
        {
            Message = new Message<Guid, JobMessage>()
            {
                Key = jobId,
                Value = new JobMessage() { Id = jobId }
            }
        };
        var jobModel = new NewJobModel
        {
            Id = jobId,
            Timeout = TimeSpan.FromSeconds(2),
            Script = "script"
        };

        var order = 0;

        var worker = CreateWorker();

        _consumer
            .Setup(m => m.Subscribe())
            .Callback(() => Assert.That(++order, Is.EqualTo(1)));
        _resourceMonitor
            .Setup(m => m.CanRunNewJobAsync(It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(++order, Is.EqualTo(2)))
            .ReturnsAsync(true);
        _consumer
            .Setup(m => m.Consume(It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(++order, Is.EqualTo(3)))
            .Returns(consumeResult);
        _jobDbContext
            .Setup(m => m.GetNewJobAsync(jobId, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(++order, Is.EqualTo(4)))
            .ReturnsAsync(jobModel);
        _runner
            .Setup(m => m.RunJob(It.Is<RunJobModel>(
                m => m.Id == jobModel.Id && m.Timeout == jobModel.Timeout && m.Script == jobModel.Script)))
            .Callback(() => Assert.That(++order, Is.EqualTo(5)));
        _jobDbContext
            .Setup(m => m.SetJobRunningAsync(jobId, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(++order, Is.EqualTo(6)))
            .Returns(Task.CompletedTask);
        _consumer
            .Setup(m => m.Commit(consumeResult))
            .Callback(() =>
            {
                Assert.That(++order, Is.EqualTo(7));
                _ = worker.StopAsync(default);
            });
        _consumer
            .Setup(m => m.Dispose())
            .Callback(() => Assert.That(++order, Is.EqualTo(8)));
        _runner
            .Setup(m => m.WaitForAllJobs())
            .Callback(() => Assert.That(++order, Is.EqualTo(9)))
            .Returns(Task.CompletedTask);

        // act
        await worker.StartAsync(default);
        await Task.Delay(TimeSpan.FromSeconds(5));

        // assert
        Assert.That(order, Is.EqualTo(9));
    }

    [Test]
    public async Task ConsumingLoop_CanNoRunJobs_ShouldNoConsumeAnyMessage()
    {
        // arrange
        var worker = CreateWorker();

        _resourceMonitor
            .Setup(m => m.CanRunNewJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // act
        await worker.StartAsync(default);
        await Task.Delay(TimeSpan.FromSeconds(5));
        await worker.StopAsync(default);

        // assert
        Assert.That(_jobDbContext.Invocations, Has.Count.Zero);
        _consumer.Verify(m => m.Consume(It.IsAny<CancellationToken>()), Times.Never);
        _runner.Verify(m => m.RunJob(It.IsAny<RunJobModel>()), Times.Never);
    }

    private ConsumerWorker CreateWorker()
    {
        return new ConsumerWorker(_consumer.Object, _runner.Object, _resourceMonitor.Object,
            _jobDbContext.Object, _consumerWorkerOptions, CreateLogger<ConsumerWorker>());
    }
}
