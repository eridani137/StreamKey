using System.Diagnostics;
using OpenTelemetry.Trace;

namespace StreamKey.Core;

public class IgnoreSignalRSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var activityName = samplingParameters.Name;

        if (activityName.Contains("SignalR", StringComparison.OrdinalIgnoreCase) ||
            activityName.Contains("BrowserExtensionHub", StringComparison.OrdinalIgnoreCase) ||
            activityName.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(SamplingDecision.Drop);
        }

        return new SamplingResult(SamplingDecision.RecordAndSample);
    }
}