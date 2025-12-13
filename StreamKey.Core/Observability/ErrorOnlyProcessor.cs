using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core.Observability;

public class ErrorOnlyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (IsExpected(activity))
        {
            Drop(activity);
            return;
        }

        if (!IsError(activity))
        {
            Drop(activity);
            return;
        }

        base.OnEnd(activity);
    }
    
    private static bool IsExpected(Activity activity)
    {
        if (activity.GetTagItem("expected_error")?.ToString() == "true") return true;

        var route = activity.GetTagItem("http.route")?.ToString();
        var status = activity.GetTagItem("http.response.status_code")?.ToString();

        if (route == "/playlist/vod" && status is "403" or "404" or "499") return true;
        if (route == "/playlist" && status is "403" or "404" or "499") return true;

        return false;
    }
    
    private static bool IsError(Activity activity)
    {
        if (activity.Status == ActivityStatusCode.Error) return true;

        if (activity.Events.Any(e => e.Name == "exception")) return true;

        var statusCode = activity.GetTagItem("http.response.status_code")?.ToString();
        if (int.TryParse(statusCode, out var code)) return code >= 400;

        return false;
    }
    
    private static void Drop(Activity activity)
    {
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }
}