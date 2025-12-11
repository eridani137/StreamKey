using System.Buffers;
using System.Text.Json;
using NATS.Client.Core;

namespace StreamKey.Shared;

public sealed class JsonNatsSerializer<T> : INatsSerialize<T>, INatsDeserialize<T>
{

    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        var writer = new Utf8JsonWriter(bufferWriter);
        
        try
        {
            JsonSerializer.Serialize(writer, value, JsonNatsDefaults.Options);
            writer.Flush();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty) return default;

        if (buffer.IsSingleSegment)
        {
            return JsonSerializer.Deserialize<T>(buffer.FirstSpan, JsonNatsDefaults.Options);
        }

        var length = (int)buffer.Length;
        var rentedArray = ArrayPool<byte>.Shared.Rent(length);
        
        try
        {
            buffer.CopyTo(rentedArray);
            return JsonSerializer.Deserialize<T>(
                rentedArray.AsSpan(0, length), 
                JsonNatsDefaults.Options
            );
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedArray);
        }
    }
}