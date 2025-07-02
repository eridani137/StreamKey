using Carter;

namespace StreamKey.Api.Endpoints;

public class PlaylistEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist");

        group.MapGet("/", (HttpContext context) =>
        {
            var queryParams = context.Request.Query;
    
            foreach (var param in queryParams)
            {
                Console.WriteLine($"{param.Key}: {param.Value}");
            }
    
            var fullQueryString = context.Request.QueryString.ToString();
            Console.WriteLine($"Full query string: {fullQueryString}");
        });
    }
}