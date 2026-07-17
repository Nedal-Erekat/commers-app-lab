using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        return await BuildSuccessResultAsync(user);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return AuthResult.Failure(["Invalid email or password."]);

        return await BuildSuccessResultAsync(user);
    }

    private async Task<AuthResult> BuildSuccessResultAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.GenerateToken(user, roles);
        return AuthResult.Success(new AuthResponse(token, user.Email!, expiresAt, roles.ToList()));
    }
}
