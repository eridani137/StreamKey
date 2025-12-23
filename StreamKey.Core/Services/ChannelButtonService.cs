using Microsoft.Extensions.Logging;
using StreamKey.Core.Mappers;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public interface IChannelButtonService
{
    Task<List<ChannelButtonEntity>> GetChannelButtons(CancellationToken cancellationToken);

    Task<Result<ChannelButtonEntity>> AddChannelButton(ChannelButtonDto dto, CancellationToken cancellationToken);

    Task<Result<ChannelButtonEntity>> RemoveChannelButton(string link, CancellationToken cancellationToken);

    Task<Result<ChannelButtonEntity>> UpdateChannelButton(ChannelButtonDto dto, CancellationToken cancellationToken);
}

public class ChannelButtonService(
    IChannelButtonRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<ChannelButtonService> logger) : IChannelButtonService
{
    public async Task<List<ChannelButtonEntity>> GetChannelButtons(CancellationToken cancellationToken)
    {
        return await repository.GetAll(cancellationToken);
    }

    public async Task<Result<ChannelButtonEntity>> AddChannelButton(ChannelButtonDto dto,
        CancellationToken cancellationToken)
    {
        if (await repository.HasEntity(dto.Link, cancellationToken))
        {
            return Result.Failure<ChannelButtonEntity>(Error.ChannelButtonAlreadyExist);
        }

        var button = dto.Map();

        await repository.Add(button, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(button);
    }

    public async Task<Result<ChannelButtonEntity>> RemoveChannelButton(string link, CancellationToken cancellationToken)
    {
        var button = await repository.GetByLink(link, cancellationToken);
        if (button is null)
        {
            return Result.Failure<ChannelButtonEntity>(Error.ChannelButtonNotFound);
        }

        repository.Delete(button);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(button);
    }

    public async Task<Result<ChannelButtonEntity>> UpdateChannelButton(ChannelButtonDto dto,
        CancellationToken cancellationToken)
    {
        var button = await repository.GetByLink(dto.Link, cancellationToken);
        if (button is null)
        {
            return Result.Failure<ChannelButtonEntity>(Error.ChannelButtonNotFound);
        }
        
        button.Html = dto.Html;
        button.Style = dto.Style;
        button.HoverStyle = dto.HoverStyle;
        button.ActiveStyle = dto.ActiveStyle;
        
        repository.Update(button);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(button);
    }
}