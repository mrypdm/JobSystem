namespace Job.Broker.Options;

/// <summary>
/// Admin options for Broker
/// </summary>
public class AdminOptions : BrokerOptions
{
    /// <summary>
    /// Count of partitions for new Topics
    /// </summary>
    public int PartitionsCount { get; set; }

    /// <summary>
    /// Replication factor for new Topics
    /// </summary>
    public short ReplicationFactor { get; set; }
}
