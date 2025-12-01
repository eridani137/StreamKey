using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core;

public class SignalRFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var rpcSystem = activity.GetTagItem("rpc.system")?.ToString();

        if (rpcSystem == "signalr")
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}