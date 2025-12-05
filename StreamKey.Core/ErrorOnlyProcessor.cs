using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core;

public class ErrorOnlyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var expectedError = activity.GetTagItem("expected_error")?.ToString() == "true";
        if (expectedError)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        var hasError = activity.Status == ActivityStatusCode.Error || 
                       activity.Events.Any(e => e.Name == "exception");
        
        var statusCode = activity.GetTagItem("http.response.status_code")?.ToString();
        if (!string.IsNullOrEmpty(statusCode) && int.TryParse(statusCode, out var code))
        {
            hasError = hasError || code >= 400;
        }
        
        if (!hasError)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }

        base.OnEnd(activity);
    }
}