using System.Net;
using Flurl.Http;
using Flurl.Http.Testing;
using Job.Contract;
using Job.WebApi.Client.Clients;
using Job.WebApi.Client.Exceptions;
using Job.WebApi.Client.Factories;
using Job.WebApi.Client.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Tests.Common;

namespace Tests.Unit.Job.WebApi;

/// <summary>
/// Tests for <see cref="JobWebApiClient"/>
/// </summary>
[TestFixture]
internal class JobWebApiClientTests : TestBase
{
    private const string BaseUrl = "http://localhost:8080";
    private HttpTest _httpTest = null;

    private readonly Mock<IFlurlClientFactory> _factory = new();

    [SetUp]
    public void SetUp()
    {
        _httpTest = new HttpTest();
        _factory.Reset();
        _factory
            .Setup(m => m.Create(It.IsAny<JobWebApiClientOptions>()))
            .Returns(new FlurlClient(BaseUrl));
    }

    [TearDown]
    public void TearDown()
    {
        _httpTest.Dispose();
    }

    [Test]
    public async Task CreateNewJob_Success_ShouldCallCorrectUrl_AndReceiveCorrectResponse()
    {
        // arrange
        var expectedRequest = new CreateJobRequest();
        var expectedJobId = Guid.NewGuid();
        _httpTest.RespondWithJson(expectedJobId);

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var actualJobId = await client.CreateNewJobAsync(expectedRequest, default);

        // assert
        Assert.That(actualJobId, Is.EqualTo(expectedJobId));
        _httpTest.ShouldHaveMadeACall()
            .WithUrlPattern($"{BaseUrl}/api/jobs")
            .WithVerb(HttpMethod.Post)
            .WithRequestJson(expectedRequest);
    }

    [Test]
    public void CreateNewJob_BadRequest_ShouldThrow()
    {
        // arrange
        var error = "Job script cannot be empty";
        var statusCode = HttpStatusCode.BadRequest;
        _httpTest.RespondWith(error, (int)statusCode);

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var exc = Assert.ThrowsAsync<JobWebApiException>(
            () => client.CreateNewJobAsync(new CreateJobRequest(), default));

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(exc.StatusCode, Is.EqualTo(statusCode));
        Assert.That(exc.Message, Is.EqualTo(error));
    }

    [Test]
    public void CreateNewJob_CallHasNotBeenMade_ShouldThrow()
    {
        // arrange
        _httpTest.SimulateException(new FlurlHttpException(new FlurlCall()
        {
            Request = new FlurlRequest(BaseUrl),
            HttpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseUrl))
        }));

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var exc = Assert.ThrowsAsync<JobWebApiException>(
            () => client.CreateNewJobAsync(new CreateJobRequest(), default));

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(exc.StatusCode, Is.Null);
        Assert.That(exc.Message, Is.EqualTo("Call to Job.WebApi failed"));
    }

    [Test]
    public void CreateNewJob_Timeout_ShouldThrow()
    {
        // arrange
        _httpTest.SimulateTimeout();

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var exc = Assert.ThrowsAsync<JobWebApiTimeoutException>(
            () => client.CreateNewJobAsync(new CreateJobRequest(), default));

        // assert
        Assert.That(exc.Message, Is.EqualTo("Call to Job.WebApi timed out"));
    }

    [Test]
    public async Task GetJobResults_Success_ShouldCallCorrectUrl_AndReceiveCorrectResponse()
    {
        // arrange
        var expectedResponse = new JobResultResponse() { Results = [0x00, 0x11] };
        var expectedJobId = Guid.NewGuid();
        _httpTest.RespondWithJson(expectedResponse);

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var actualResponse = await client.GetJobResultsAsync(expectedJobId, default);

        // assert
        Assert.That(actualResponse.Results, Is.EqualTo(expectedResponse.Results).AsCollection);
        _httpTest.ShouldHaveMadeACall()
            .WithUrlPattern($"{BaseUrl}/api/jobs/{expectedJobId}")
            .WithVerb(HttpMethod.Get);
    }

    [Test]
    public void GetJobResults_NotFound_ShouldThrow()
    {
        // arrange
        var error = "Cannot found results for job '00000000-0000-0000-0000-000000000000'";
        var statusCode = HttpStatusCode.NotFound;
        _httpTest.RespondWith(error, (int)statusCode);

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var exc = Assert.ThrowsAsync<JobWebApiException>(
            () => client.GetJobResultsAsync(Guid.Empty, default));

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(exc.StatusCode, Is.EqualTo(statusCode));
        Assert.That(exc.Message, Is.EqualTo(error));
    }

    [Test]
    public void GetJobResults_CallHasNotBeenMade_ShouldThrow()
    {
        // arrange
        _httpTest.SimulateException(new FlurlHttpException(new FlurlCall()
        {
            Request = new FlurlRequest(BaseUrl),
            HttpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseUrl))
        }));

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var exc = Assert.ThrowsAsync<JobWebApiException>(
            () => client.GetJobResultsAsync(Guid.Empty, default));

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(exc.StatusCode, Is.Null);
        Assert.That(exc.Message, Is.EqualTo("Call to Job.WebApi failed"));
    }

    [Test]
    public void GetJobResults_Timeout_ShouldThrow()
    {
        // arrange
        _httpTest.SimulateTimeout();

        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var exc = Assert.ThrowsAsync<JobWebApiTimeoutException>(
            () => client.CreateNewJobAsync(new CreateJobRequest(), default));

        // assert
        Assert.That(exc.Message, Is.EqualTo("Call to Job.WebApi timed out"));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(new JobWebApiClientOptions());
        builder.Services.AddSingleton(_factory.Object);
        builder.Services.AddTransient<JobWebApiClient>();
    }
}
