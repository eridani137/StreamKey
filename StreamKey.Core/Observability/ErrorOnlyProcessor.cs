using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core.Observability;

public sealed class ErrorOnlyProcessor : BaseProcessor<Activity>
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
        var status = activity.GetTagItem("http.response.status_code")?.ToString();

        // ---------- AspNetCore ----------
        var route = activity.GetTagItem("http.route")?.ToString()?.TrimEnd('/');

        if (route is "/playlist" or "/playlist/vod" &&
            status is "403" or "404" or "499")
        {
            return true;
        }

        // ---------- HttpClient (usher) ----------
        var host = activity.GetTagItem("server.address")?.ToString();

        if (host == "usher.ttvnw.net" && status is "403" or "404" or "499")
        {
            return true;
        }

        return false;
    }

    private static bool IsError(Activity activity)
    {
        if (activity.Status == ActivityStatusCode.Error)
            return true;

        if (activity.Events.Any(e => e.Name == "exception"))
            return true;

        var statusCode = activity.GetTagItem("http.response.status_code")?.ToString();
        return int.TryParse(statusCode, out var code) && code >= 400;
    }

    private static void Drop(Activity activity)
    {
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }
}