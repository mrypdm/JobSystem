using Job.Broker;
using Job.Contract;
using Job.Database.Contexts;
using Job.WebApi.Controllers;
using Job.WebApi.Options;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Broker.Abstractions;
using Tests.Unit;

namespace Job.WebApi.Tests;

/// <summary>
/// Tests for <see cref="JobsController"/>
/// </summary>
[TestFixture]
internal class JobsControllerTests : UnitTestBase
{
    private readonly Mock<IJobDbContext> _jobDbContext = new();
    private readonly Mock<IJobProducer<Guid, JobMessage>> _jobProducer = new();
    private readonly JobsControllerOptions _jobsControllerOptions = new()
    {
        MaxTimeout = TimeSpan.FromSeconds(60),
        DefaultTimeout = TimeSpan.FromSeconds(5)
    };

    [SetUp]
    public void SetUp()
    {
        _jobDbContext.Reset();
        _jobProducer.Reset();
    }

    [Test]
    public async Task AddNewJobAsync_NullRequest_ShouldReturnBadRequest()
    {
        // arrange
        var controller = CreateController();

        // act
        var result = await controller.AddNewJobAsync(null, default);

        // assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddNewJobAsync_EmptyScript_ShouldReturnBadRequest()
    {
        // arrange
        var request = new CreateJobRequest() { Script = "" };
        var controller = CreateController();

        // act
        var result = await controller.AddNewJobAsync(request, default);

        // assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddNewJobAsync_NotBase64Script_ShouldReturnBadRequest()
    {
        // arrange
        var request = new CreateJobRequest() { Script = "not_base_64" };
        var controller = CreateController();

        // act
        var result = await controller.AddNewJobAsync(request, default);

        // assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddNewJobAsync_LargeTimeout_ShouldReturnBadRequest()
    {
        // arrange
        var request = new CreateJobRequest() { Timeout = TimeSpan.FromDays(100) };
        var controller = CreateController();

        // act
        var result = await controller.AddNewJobAsync(request, default);

        // assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddNewJobAsync_EmptyTimeout_EmptyId_ShouldUseDefaultTimeout_ShouldGenerateId()
    {
        // arrange
        NewJobModel actualJob = null;
        _jobDbContext
            .Setup(m => m.AddNewJobAsync(It.IsAny<NewJobModel>(), It.IsAny<CancellationToken>()))
            .Callback((NewJobModel model, CancellationToken _) => actualJob = model);

        var request = new CreateJobRequest() { Script = Convert.ToBase64String([0]) };
        var controller = CreateController();

        // act
        var result = await controller.AddNewJobAsync(request, default);

        // assert
        Assert.That(actualJob, Is.Not.Null);
        Assert.That(result.Value, Is.Not.EqualTo(Guid.Empty));
        Assert.That(actualJob.Id, Is.EqualTo(result.Value));
        Assert.That(actualJob.Timeout, Is.EqualTo(_jobsControllerOptions.DefaultTimeout));
    }

    [Test]
    public async Task AddNewJobAsync_ShouldAddToDatabase_ThenSendToBroker()
    {
        // arrange
        var request = new CreateJobRequest()
        {
            Id = Guid.NewGuid(),
            Timeout = TimeSpan.FromSeconds(1),
            Script = Convert.ToBase64String([0])
        };

        var order = 0;
        NewJobModel actualJob = null;

        _jobDbContext
            .Setup(m => m.AddNewJobAsync(It.Is<NewJobModel>(m => m.Id == request.Id), It.IsAny<CancellationToken>()))
            .Callback((NewJobModel model, CancellationToken _) =>
            {
                actualJob = model;
                Assert.That(++order, Is.EqualTo(1));
            });
        _jobProducer
            .Setup(m => m.PublishAsync(It.Is<JobMessage>(m => m.Id == request.Id), It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(++order, Is.EqualTo(2)));

        var controller = CreateController();

        // act
        var result = await controller.AddNewJobAsync(request, default);

        // assert
        Assert.That(order, Is.EqualTo(2));
        Assert.That(result.Value, Is.EqualTo(request.Id));
        Assert.That(actualJob.Id, Is.EqualTo(request.Id));
        Assert.That(actualJob.Timeout, Is.EqualTo(request.Timeout));
        Assert.That(actualJob.Script, Is.EqualTo(request.Script));

        _jobDbContext.Verify(
            m => m.AddNewJobAsync(It.Is<NewJobModel>(m => m.Id == request.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        _jobProducer.Verify(
            m => m.PublishAsync(It.Is<JobMessage>(m => m.Id == request.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetJobResultsAsync_ShouldGetResultsFromDatabase()
    {
        // arrange
        var id = Guid.NewGuid();
        var results = new JobResultResponse();

        _jobDbContext
            .Setup(m => m.GetJobResults(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var controller = CreateController();

        // act
        var result = await controller.GetJobResultsAsync(id, default);

        // assert
        Assert.That(result.Value, Is.SameAs(results));
        _jobDbContext.Verify(
            m => m.GetJobResults(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetJobResultsAsync_NullResults_ShouldReturnNotFound()
    {
        // arrange
        var id = Guid.NewGuid();

        _jobDbContext
            .Setup(m => m.GetJobResults(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobResultResponse)null);

        var controller = CreateController();

        // act
        var result = await controller.GetJobResultsAsync(id, default);

        // assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        _jobDbContext.Verify(
            m => m.GetJobResults(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private JobsController CreateController()
    {
        return new JobsController(_jobDbContext.Object, _jobProducer.Object, _jobsControllerOptions,
            CreateLogger<JobsController>());
    }
}
