using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core;

public class ErrorOnlyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (IsSuccess(activity))
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }
        
        base.OnEnd(activity);
    }

    private static bool IsSuccess(Activity activity)
    {
        if (activity.Status == ActivityStatusCode.Ok) return true;

        if (activity.GetTagItem("http.status_code") is string statusCodeStr &&
            int.TryParse(statusCodeStr, out var statusCode) && 
            statusCode is >= 200 and < 300) return true;

        if (activity.GetTagItem("otel.status_code")?.ToString() == "OK") return true;

        return false;
    }
}