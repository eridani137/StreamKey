using System.Net;

namespace StreamKey.Core.Observability;

public class FilterExpectedErrorStatusCodeHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
            {
                using var activity = System.Diagnostics.Activity.Current;
                activity?.SetTag("expected_not_found", "true");
                break;
            }
            case HttpStatusCode.Forbidden:
            {
                using var activity = System.Diagnostics.Activity.Current;
                activity?.SetTag("expected_forbidden", "true");
                break;
            }
        }

        return response;
    }
}