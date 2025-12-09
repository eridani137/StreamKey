namespace StreamKey.Shared.Events;

public class RpcRequestEnvelope
{
    public Guid RequestId { get; set; }
    public required string Payload { get; set; }
}