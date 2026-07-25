namespace Backend.Models.Auth;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpiresAt { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime RefreshTokenExpiresAt { get; init; }

    public required UserSummary User { get; init; }
}
