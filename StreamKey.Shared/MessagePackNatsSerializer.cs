using System.Buffers;
using MessagePack;
using NATS.Client.Core;

namespace StreamKey.Shared;

public class MessagePackNatsSerializer<T> : INatsSerialize<T>, INatsDeserialize<T>
{
    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        var bytes = MessagePackSerializer.Serialize(value);
        bufferWriter.Write(bytes);
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty) return default;

        var bytes = buffer.ToArray();
        return MessagePackSerializer.Deserialize<T>(bytes);
    }
}