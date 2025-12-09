namespace StreamKey.Shared.Events;

public class RpcResponseEnvelope
{
    public Guid RequestId { get; set; }
    public required string Payload { get; set; }
}