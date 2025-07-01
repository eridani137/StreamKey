using StreamKey.Application.DTOs;
using StreamKey.Application.Results;

namespace StreamKey.Application.Interfaces;

public interface ITwitchService
{
    Task<Result<StreamResponseDto>> GetStreamSource(string username);
}