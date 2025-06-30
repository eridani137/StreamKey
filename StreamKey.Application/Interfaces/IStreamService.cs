namespace StreamKey.Application.Interfaces;

public interface IStreamService
{
    Task<string?> GetSource(string username);
}