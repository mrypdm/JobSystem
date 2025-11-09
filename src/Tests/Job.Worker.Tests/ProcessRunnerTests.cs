using System.Diagnostics;
using System.Runtime.InteropServices;
using Job.Worker.Processes;

namespace Job.Worker.Tests;

/// <summary>
/// Tests for <see cref="ProcessRunner"/>
/// </summary>
[TestFixture]
internal class ProcessRunnerTests : TestBase
{
    [Test]
    public async Task RunProcess_ShouldStartAndWait()
    {
        // arrange
        var runner = CreateRunner();

        // act
        var sw = Stopwatch.StartNew();
        await runner.RunProcessAsync(GetCommandForOs(), Environment.CurrentDirectory, default);
        sw.Stop();

        Assert.That(sw.Elapsed.TotalSeconds, Is.EqualTo(5.0).Within(1.0));
    }

    private string[] GetCommandForOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ["cmd", "/c", "timeout /t 5"];
        }
        else
        {
            return ["bash", "-c", "sleep 5"];
        }
    }

    private ProcessRunner CreateRunner()
    {
        return new ProcessRunner(CreateLogger<ProcessRunner>());
    }
}
