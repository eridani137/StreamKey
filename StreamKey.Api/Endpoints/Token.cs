using System.Net.Http.Headers;
using Carter;
using StreamKey.Shared;

namespace StreamKey.Api.Endpoints;

public class Token : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/token");

        group.MapPost("/gql", async (IHttpClientFactory clientFactory, HttpContext context) =>
            {
                var client = clientFactory.CreateClient(ApplicationConstants.UsherClientName);

                var request = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.GqlUrl)
                {
                    Content = new StreamContent(context.Request.Body)
                };

                CopyRequestHeaders(context.Request, request);

                CopyContentType(context.Request, request);

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted);

                CopyResponseHeaders(context.Response, response);

                await CopyResponseBodyAsync(response, context.Response, context.RequestAborted);

                return Results.Empty;
            })
            .DisableAntiforgery();
    }

    private static void CopyRequestHeaders(HttpRequest request, HttpRequestMessage requestMessage)
    {
        foreach (var header in request.Headers)
        {
            if (!IsSystemHeader(header.Key))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private static void CopyContentType(HttpRequest request, HttpRequestMessage requestMessage)
    {
        if (request.ContentType != null)
        {
            requestMessage.Content!.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        }
    }

    private static void CopyResponseHeaders(HttpResponse response, HttpResponseMessage responseMessage)
    {
        response.StatusCode = (int)responseMessage.StatusCode;

        foreach (var header in responseMessage.Headers)
        {
            response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    private static async Task CopyResponseBodyAsync(HttpResponseMessage source, HttpResponse destination,
        CancellationToken cancellationToken)
    {
        await source.Content.CopyToAsync(destination.Body, cancellationToken);
    }

    private static bool IsSystemHeader(string headerName) =>
        headerName.Equals("host", StringComparison.OrdinalIgnoreCase) ||
        headerName.Equals("connection", StringComparison.OrdinalIgnoreCase);
}