namespace Job.Broker;

/// <summary>
/// Message for start Job at Worker
/// </summary>
public class JobMessage
{
    /// <summary>
    /// Job Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Serializes message to byte array
    /// </summary>
    public byte[] ToByteArray()
    {
        return Id.ToByteArray();
    }
}
