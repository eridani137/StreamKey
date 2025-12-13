using System.Net;

namespace StreamKey.Core.Observability;

public class FilterNotFoundHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            using var activity = System.Diagnostics.Activity.Current;
            activity?.SetTag("expected_not_found", "true");
        }
        
        return response;
    }
}