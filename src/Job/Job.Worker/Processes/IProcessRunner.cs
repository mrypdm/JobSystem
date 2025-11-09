namespace Job.Worker.Processes;

/// <summary>
/// Process runner
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Run process with <paramref name="command"/> at <paramref name="workingDirectory"/>
    /// </summary>
    Task RunProcessAsync(string[] command, string workingDirectory, CancellationToken cancellationToken);
}
