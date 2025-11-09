using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Processes;

/// <inheritdoc />
public class ProcessRunner(ILogger<ProcessRunner> logger) : IProcessRunner
{
    /// <inheritdoc />
    public async Task RunProcessAsync(string[] command, string workingDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(command[0], command.Skip(1))
            {
                WorkingDirectory = workingDirectory
            },
        };
        process.Start();

        logger.LogInformation("Process started with command [{@Command}] in [{WorkingDir}]", command, workingDirectory);

        await process.WaitForExitAsync(cancellationToken);
    }
}
