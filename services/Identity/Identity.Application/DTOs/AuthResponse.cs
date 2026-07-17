namespace Identity.Application.DTOs;

public record AuthResponse(string Token, string Email, DateTime ExpiresAt, IReadOnlyList<string> Roles);
