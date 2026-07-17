namespace Identity.Application.DTOs;

public record AuthResult(bool Succeeded, AuthResponse? Response, IReadOnlyList<string> Errors)
{
    public static AuthResult Success(AuthResponse response) => new(true, response, Array.Empty<string>());
    public static AuthResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToList());
}
