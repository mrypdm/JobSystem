using System;
using Job.Worker.Workers;

namespace Job.Worker.Options;

/// <summary>
/// Options for <see cref="ConsumerWorker"/>
/// </summary>
public class ConsumerWorkerOptions
{
    /// <summary>
    /// Delay between consume tries
    /// </summary>
    public TimeSpan IterationDeplay { get; set; }
}
