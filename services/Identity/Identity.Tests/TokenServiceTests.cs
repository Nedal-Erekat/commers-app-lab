using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Identity.Domain;
using Identity.Domain.Entities;
using Identity.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace Identity.Tests;

public class TokenServiceTests
{
    private static TokenService CreateSut(int expiryMinutes = 60)
    {
        var settings = new JwtSettings
        {
            Key = "unit-test-signing-key-at-least-32-bytes-long!!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = expiryMinutes
        };
        return new TokenService(Options.Create(settings));
    }

    [Fact]
    public void GenerateToken_IncludesEmailAndRoleClaims()
    {
        var sut = CreateSut();
        var user = new ApplicationUser { Id = "user-1", Email = "alice@example.com" };

        var (token, _) = sut.GenerateToken(user, [Roles.Customer]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("alice@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == Roles.Customer);
    }

    [Fact]
    public void GenerateToken_SetsIssuerAndAudience()
    {
        var sut = CreateSut();
        var user = new ApplicationUser { Id = "user-1", Email = "alice@example.com" };

        var (token, _) = sut.GenerateToken(user, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Equal("test-audience", jwt.Audiences.Single());
    }

    [Fact]
    public void GenerateToken_ExpiresAtMatchesConfiguredMinutes()
    {
        var sut = CreateSut(expiryMinutes: 30);
        var user = new ApplicationUser { Id = "user-1", Email = "alice@example.com" };

        var before = DateTime.UtcNow;
        var (_, expiresAt) = sut.GenerateToken(user, []);

        Assert.InRange(expiresAt, before.AddMinutes(29), before.AddMinutes(31));
    }
}
