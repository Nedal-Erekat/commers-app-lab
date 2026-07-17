using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IList<string> roles);
}
