using System.Buffers;
using NATS.Client.Core;
using ProtoBuf;

namespace StreamKey.Shared;

public class ProtobufNatsSerializer<T> : INatsSerialize<T>, INatsDeserialize<T>
{
    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, value);
        bufferWriter.Write(ms.ToArray());
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty) return default;

        using var ms = new MemoryStream(buffer.ToArray());
        return Serializer.Deserialize<T>(ms);
    }
}