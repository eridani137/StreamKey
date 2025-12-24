using Microsoft.Extensions.Logging;
using StreamKey.Core.Mappers;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public interface IButtonService
{
    Task<List<ButtonEntity>> GetButtons(CancellationToken cancellationToken);

    Task<Result<ButtonEntity>> AddButton(ButtonDto dto, CancellationToken cancellationToken);

    Task<Result<ButtonEntity>> RemoveButton(string link, CancellationToken cancellationToken);

    Task<Result<ButtonEntity>> UpdateButton(ButtonDto dto, CancellationToken cancellationToken);
}

public class ButtonService(
    IButtonRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<ButtonService> logger) : IButtonService
{
    public async Task<List<ButtonEntity>> GetButtons(CancellationToken cancellationToken)
    {
        return await repository.GetAll(cancellationToken);
    }

    public async Task<Result<ButtonEntity>> AddButton(ButtonDto dto,
        CancellationToken cancellationToken)
    {
        if (await repository.HasEntity(dto.Link, cancellationToken))
        {
            return Result.Failure<ButtonEntity>(Error.ButtonAlreadyExist);
        }

        var button = dto.Map();

        await repository.Add(button, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(button);
    }

    public async Task<Result<ButtonEntity>> RemoveButton(string link, CancellationToken cancellationToken)
    {
        var button = await repository.GetByLink(link, cancellationToken);
        if (button is null)
        {
            return Result.Failure<ButtonEntity>(Error.ButtonNotFound);
        }

        repository.Delete(button);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(button);
    }

    public async Task<Result<ButtonEntity>> UpdateButton(ButtonDto dto,
        CancellationToken cancellationToken)
    {
        var button = await repository.GetByLink(dto.Link, cancellationToken);
        if (button is null)
        {
            return Result.Failure<ButtonEntity>(Error.ButtonNotFound);
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