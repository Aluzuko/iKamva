using Backend.Domain.Entities;

namespace Backend.Infrastructure.Auth;

public interface IUserSessionService
{
    /// <summary>Starts a new session for a freshly authenticated user and returns the raw refresh token.</summary>
    Task<(UserSession Session, string RefreshToken)> StartSessionAsync(
        User user, string? ipAddress, string? userAgent, CancellationToken ct = default);

    /// <summary>
    /// Validates a refresh token and rotates it — the old session is revoked and a new
    /// one issued for the same user. Returns null if the token is invalid, expired, or revoked.
    /// </summary>
    Task<(UserSession Session, User User, string RefreshToken)?> RotateRefreshTokenAsync(
        string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct = default);

    Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken ct = default);

    /// <summary>Revokes every active session for a user — "sign out everywhere".</summary>
    Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct = default);

    /// <summary>Stores an arbitrary key/value pair scoped to a session (persisted as jsonb).</summary>
    Task SetVariableAsync(Guid sessionId, string key, string value, CancellationToken ct = default);

    Task<string?> GetVariableAsync(Guid sessionId, string key, CancellationToken ct = default);
}
