using Confluent.Kafka;

namespace Job.Broker.Converters;

/// <summary>
/// Converter for <see cref="Guid"/>
/// </summary>
public class GuidConverter : ISerializer<Guid>, IDeserializer<Guid>
{
    /// <inheritdoc />
    public byte[] Serialize(Guid data, SerializationContext context)
    {
        return data.ToByteArray();
    }

    /// <inheritdoc />
    public Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return isNull ? Guid.Empty : new Guid(data);
    }
}
