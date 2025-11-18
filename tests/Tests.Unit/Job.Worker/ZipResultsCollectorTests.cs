using Job.Worker.Collectors;
using Job.Worker.Models;
using Job.Worker.Processes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Tests.Common;

namespace Tests.Unit.Job.Worker;

/// <summary>
/// Tests for <see cref="ZipResultsCollector"/>
/// </summary>
[TestFixture]
internal class ZipResultsCollectorTests : TestBase
{
    private readonly Mock<IProcessRunner> _runner = new();

    [SetUp]
    public void SetUp()
    {
        _runner.Reset();
    }

    [Test]
    public void CollectResults_NullJob_Throw()
    {
        // arrange
        var collector = Services.GetRequiredService<ZipResultsCollector>();

        // act & assert
        Assert.ThrowsAsync<ArgumentNullException>(() => collector.CollectResultsAsync(null));
    }

    [Test]
    public void CollectResults_JobDirectoryNotSet_Throw()
    {
        // arrange
        var collector = Services.GetRequiredService<ZipResultsCollector>();

        // act & assert
        Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectResultsAsync(new RunJobModel()));
    }

    [Test]
    public async Task CollectResults_ShouldCallZip_AndSaveToModel()
    {
        // arrange
        var expectedCommand = new string[] { "zip", "results.zip", "stdout.txt", "stderr.txt" };
        var expectedResults = new byte[] { 0x00, 0x11 };

        var jobModel = new RunJobModel
        {
            Id = Guid.NewGuid(),
            Directory = "TestData"
        };

        File.WriteAllBytes(Path.Combine("TestData", "results.zip"), expectedResults);

        var collector = Services.GetRequiredService<ZipResultsCollector>();

        // act
        await collector.CollectResultsAsync(jobModel);

        // assert
        Assert.That(jobModel.Results, Is.EqualTo(expectedResults).AsCollection);

        _runner.Verify(
            m => m.RunProcessAsync(It.Is<string[]>(m => m.SequenceEqual(expectedCommand)), jobModel.Directory,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(_runner.Object);
        builder.Services.AddTransient<ZipResultsCollector>();
    }
}
