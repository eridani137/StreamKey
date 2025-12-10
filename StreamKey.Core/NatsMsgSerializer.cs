using System.Buffers;
using NATS.Client.Core;

namespace StreamKey.Core;

public class NatsMsgSerializer<T>(INatsDeserialize<T> inner) : INatsDeserialize<NatsMsg<T>>
{
    public NatsMsg<T> Deserialize(in ReadOnlySequence<byte> buffer)
    {
        var t = inner.Deserialize(buffer);
        return new NatsMsg<T> { Data = t };
    }
}