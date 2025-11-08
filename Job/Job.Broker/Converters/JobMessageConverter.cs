using Confluent.Kafka;

namespace Job.Broker.Converters;

/// <summary>
/// Converter for <see cref="JobMessage"/>
/// </summary>
public class JobMessageConverter : ISerializer<JobMessage>, IDeserializer<JobMessage>
{
    /// <inheritdoc />
    public byte[] Serialize(JobMessage data, SerializationContext context)
    {
        return data.ToByteArray();
    }

    /// <inheritdoc />
    public JobMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return new JobMessage
        {
            Id = isNull ? Guid.Empty : new Guid(data)
        };
    }
}
