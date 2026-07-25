using Backend.Domain.Entities;

namespace Backend.Infrastructure.Auth;

public interface IJwtTokenService
{
    /// <summary>Issues a short-lived, stateless access token for the given user.</summary>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>Generates a cryptographically random opaque refresh token (raw, not hashed).</summary>
    string GenerateRefreshToken();

    /// <summary>Hashes a refresh token for storage/lookup — the raw value is never persisted.</summary>
    string HashRefreshToken(string refreshToken);
}
