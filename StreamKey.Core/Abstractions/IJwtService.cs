using StreamKey.Shared.Entities;

namespace StreamKey.Core.Abstractions;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IEnumerable<string> roleNames);
}