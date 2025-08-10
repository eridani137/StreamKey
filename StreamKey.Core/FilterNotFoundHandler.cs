using System.Net;

namespace StreamKey.Core;

public class FilterNotFoundHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            using var activity = System.Diagnostics.Activity.Current;
            activity?.SetTag("expected_not_found", "true");
        }
        
        return response;
    }
}