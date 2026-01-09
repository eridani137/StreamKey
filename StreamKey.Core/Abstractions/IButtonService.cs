using StreamKey.Core.Results;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Abstractions;

public interface IButtonService
{
    Task<List<ButtonEntity>> GetButtons(CancellationToken cancellationToken);
    
    Task<List<ButtonEntity>> GetButtonsByPosition(ButtonPosition position, CancellationToken cancellationToken);

    Task<Result<ButtonEntity>> AddButton(ButtonDto dto, CancellationToken cancellationToken);

    Task<Result<ButtonEntity>> RemoveButton(Guid id, CancellationToken cancellationToken);

    Task<Result<ButtonEntity>> UpdateButton(ButtonDto dto, CancellationToken cancellationToken);
}