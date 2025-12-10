using System.Buffers;
using System.Text.Json;
using NATS.Client.Core;

namespace StreamKey.Shared;

public class JsonNatsSerializer<T> : INatsSerialize<T>, INatsDeserialize<T>
{
    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        var writer = new ArrayBufferWriter<byte>();
        JsonSerializer.Serialize(value);
        bufferWriter.Write(writer.WrittenSpan);
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty) return default;

        var span = buffer.ToArray();
        return JsonSerializer.Deserialize<T>(span);
    }
}