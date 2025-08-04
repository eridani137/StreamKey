using Carter;
using Microsoft.AspNetCore.Mvc;
using StreamKey.Application.DTOs;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Mappers;

namespace StreamKey.Api.Endpoints;

public class Channel : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/channel")
            .RequireAuthorization();

        group.MapGet("",
                async (IChannelService service) =>
                {
                    var channels = await service.GetChannels();

                    Results.Ok(channels.Map());
                })
            .Produces<List<ChannelDto>>();

        group.MapPost("",
                async ([FromBody] ChannelDto dto, IChannelService service) =>
                {
                    var result = await service.AddChannel(dto);

                    if (!result.IsSuccess)
                    {
                        Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    Results.Ok(result.Value);
                })
            .Produces<ChannelDto>();

        group.MapDelete("",
                async ([FromBody] ChannelDto dto, IChannelService service) =>
                {
                    var result = await service.RemoveChannel(dto);

                    if (!result.IsSuccess)
                    {
                        Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    Results.Ok(result.Value);
                })
            .Produces<ChannelDto>();
        ;
    }
}