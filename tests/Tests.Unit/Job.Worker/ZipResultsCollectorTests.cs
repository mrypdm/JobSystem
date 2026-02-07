using System.IO.Compression;
using Job.Worker.Collectors;
using Job.Worker.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tests.Common;

namespace Tests.Unit.Job.Worker;

/// <summary>
/// Tests for <see cref="ZipResultsCollector"/>
/// </summary>
[TestFixture]
internal class ZipResultsCollectorTests : TestBase
{
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
    public async Task CollectResults_ShouldCreateZipArchive()
    {
        // arrange
        var jobModel = new RunJobModel
        {
            Id = Guid.NewGuid(),
            Directory = "TestData/results-collector"
        };

        var collector = Services.GetRequiredService<ZipResultsCollector>();

        // act
        await collector.CollectResultsAsync(jobModel);

        // assert
        using var bytes = new MemoryStream(jobModel.Results);
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read, leaveOpen: true);

        Assert.That(zip.Entries, Has.Count.EqualTo(2));

        using var _ = Assert.EnterMultipleScope();
        Assert.That(zip.Entries.ElementAt(0).Name, Is.EqualTo("stdout.txt"));
        Assert.That(ReadZipEntry(zip.Entries.ElementAt(0)), Is.EqualTo("stdout"));
        Assert.That(zip.Entries.ElementAt(1).Name, Is.EqualTo("stderr.txt"));
        Assert.That(ReadZipEntry(zip.Entries.ElementAt(1)), Is.EqualTo("stderr"));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddTransient<ZipResultsCollector>();
    }

    private static string ReadZipEntry(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        return reader.ReadToEnd().Trim();
    }
}
