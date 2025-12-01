using System.Diagnostics;
using OpenTelemetry.Trace;

namespace StreamKey.Core;

public class IgnoreSignalRSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        var spanName = parameters.Name;

        if (parameters.Kind == ActivityKind.Server &&
            spanName.StartsWith("StreamKey.Core.Hubs.", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(SamplingDecision.Drop);
        }

        return new SamplingResult(SamplingDecision.RecordAndSample);
    }
}