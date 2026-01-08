using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class ButtonService(
    IButtonRepository repository,
    IUnitOfWork unitOfWork,
    INatsConnection nats,
    JsonNatsSerializer<int> serializer
) : IButtonService
{
    public async Task<List<ButtonEntity>> GetButtons(CancellationToken cancellationToken)
    {
        return await repository.GetAll(cancellationToken);
    }

    public async Task<Result<ButtonEntity>> AddButton(ButtonDto dto,
        CancellationToken cancellationToken)
    {
        var button = dto.Map();

        await repository.Add(button, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        await nats.PublishAsync(NatsKeys.InvalidateButtonsCache, (int)button.Position, serializer: serializer, cancellationToken: cancellationToken);

        return Result.Success(button);
    }

    public async Task<Result<ButtonEntity>> RemoveButton(Guid id, CancellationToken cancellationToken)
    {
        var button = await repository.GetById(id, cancellationToken);
        if (button is null)
        {
            return Result.Failure<ButtonEntity>(Error.ButtonNotFound);
        }

        repository.Delete(button);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        await nats.PublishAsync(NatsKeys.InvalidateButtonsCache, (int)button.Position, serializer: serializer, cancellationToken: cancellationToken);

        return Result.Success(button);
    }

    public async Task<Result<ButtonEntity>> UpdateButton(ButtonDto dto,
        CancellationToken cancellationToken)
    {
        var button = await repository.GetById(dto.Id, cancellationToken);
        if (button is null)
        {
            return Result.Failure<ButtonEntity>(Error.ButtonNotFound);
        }

        button.Html = dto.Html;
        button.Style = dto.Style;
        button.HoverStyle = dto.HoverStyle;
        button.ActiveStyle = dto.ActiveStyle;
        button.Link = dto.Link;
        button.IsEnabled = dto.IsEnabled;

        repository.Update(button);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        await nats.PublishAsync(NatsKeys.InvalidateButtonsCache, (int)button.Position, serializer: serializer, cancellationToken: cancellationToken);

        return Result.Success(button);
    }
}