using Backend.Domain.Entities;

namespace Backend.Infrastructure.Auth;

public interface IUserSessionService
{
    Task<(UserSession Session, string RefreshToken)> StartSessionAsync(
        User user, string? ipAddress, string? userAgent, CancellationToken ct = default);

    Task<(UserSession Session, User User, string RefreshToken)?> RotateRefreshTokenAsync(
        string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct = default);

    Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken ct = default);

    Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct = default);

    Task SetVariableAsync(Guid sessionId, string key, string value, CancellationToken ct = default);

    Task<string?> GetVariableAsync(Guid sessionId, string key, CancellationToken ct = default);
}
