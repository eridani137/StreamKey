using System.Diagnostics;
using OpenTelemetry;

namespace StreamKey.Core;

public class IgnorePathProcessor(params string[]? ignoredPaths) : BaseProcessor<Activity>
{
    private readonly string[] _ignoredPaths = ignoredPaths ?? [];

    public override void OnEnd(Activity activity)
    {
        var path = GetPath(activity);
        if (path == null)
            return;

        foreach (var ignore in _ignoredPaths)
        {
            if (path.Equals(ignore, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(ignore, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        base.OnEnd(activity);
    }

    private static string? GetPath(Activity activity)
    {
        return activity.GetTagItem("http.route")?.ToString()
               ?? activity.GetTagItem("url.path")?.ToString()
               ?? activity.GetTagItem("http.target")?.ToString();
    }
}