using System;

namespace Job.Broker.Options;

/// <summary>
/// Options for <see cref="JobConsumer"/>
/// </summary>
public class ConsumerOptions : BrokerOptions
{
    /// <summary>
    /// Group Id
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Delay between consume tries
    /// </summary>
    public TimeSpan IterationDeplay { get; set; }
}
