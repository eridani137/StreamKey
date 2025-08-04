using Carter;
using Microsoft.AspNetCore.Mvc;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Filters;
using StreamKey.Core.Mappers;

namespace StreamKey.Api.Endpoints;

public class Channel : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/channels")
            .WithTags("Работа с каналами")
            .RequireAuthorization();

        group.MapGet("",
                async (IChannelService service) =>
                {
                    var channels = await service.GetChannels();
                    var mapped = channels.Map();
                    
                    return Results.Ok(mapped);
                })
            .Produces<List<ChannelDto>>()
            .AllowAnonymous();

        group.MapPost("",
                async (ChannelDto dto, IChannelService service) =>
                {
                    var result = await service.AddChannel(dto);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .AddEndpointFilter<ValidationFilter<ChannelDto>>()
            .Produces<ChannelDto>();

        group.MapDelete("/{channelName}",
                async (string channelName, IChannelService service) =>
                {
                    var result = await service.RemoveChannel(channelName);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .Produces<ChannelDto>();

        group.MapPut("",
            async (ChannelDto dto, IChannelService service) =>
            {
                var result = await service.UpdateChannel(dto);

                if (!result.IsSuccess)
                {
                    return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                }
                
                return Results.Ok(result.Value.Map());
            })
            .Produces<ChannelDto>();
    }
}