using System.Diagnostics;
using System.Runtime.InteropServices;
using Job.Worker.Processes;
using Microsoft.Extensions.DependencyInjection;
using Tests.Unit;

namespace Job.Worker.Tests;

/// <summary>
/// Tests for <see cref="ProcessRunner"/>
/// </summary>
[TestFixture]
internal class ProcessRunnerTests : UnitTestBase
{
    [Test]
    public async Task RunProcess_ShouldStartAndWait()
    {
        // arrange
        var runner = Services.GetRequiredService<ProcessRunner>();

        // act
        var sw = Stopwatch.StartNew();
        await runner.RunProcessAsync(GetCommandForOs(), Environment.CurrentDirectory, default);
        sw.Stop();

        Assert.That(sw.Elapsed.TotalSeconds, Is.EqualTo(5.0).Within(1.0));
    }

    private static string[] GetCommandForOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ["powershell", "-c", "Start-Sleep -Seconds 5"];
        }
        else
        {
            return ["bash", "-c", "sleep 5"];
        }
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddTransient<ProcessRunner>();
    }
}
