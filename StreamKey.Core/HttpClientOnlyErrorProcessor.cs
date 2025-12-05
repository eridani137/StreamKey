using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core;

public class HttpClientOnlyErrorProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var source = activity.Source.Name;

        if (source != "System.Net.Http") return;

        var expectedError = activity.GetTagItem("expected_error")?.ToString() == "true";
        if (expectedError) return;

        if (activity.Status == ActivityStatusCode.Error)
        {
            base.OnEnd(activity);
            return;
        }

        if (activity.Events.Any(e => e.Name == "exception"))
        {
            base.OnEnd(activity);
            return;
        }
    }
}