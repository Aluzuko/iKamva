using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Auth;

public class UserSessionService : IUserSessionService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly JwtOptions _options;

    public UserSessionService(AppDbContext db, IJwtTokenService jwt, IOptions<JwtOptions> options)
    {
        _db = db;
        _jwt = jwt;
        _options = options.Value;
    }

    public async Task<(UserSession Session, string RefreshToken)> StartSessionAsync(
        User user, string? ipAddress, string? userAgent, CancellationToken ct = default)
    {
        var refreshToken = _jwt.GenerateRefreshToken();

        var session = new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = _jwt.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return (session, refreshToken);
    }

    public async Task<(UserSession Session, User User, string RefreshToken)?> RotateRefreshTokenAsync(
        string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct = default)
    {
        var hash = _jwt.HashRefreshToken(refreshToken);

        var existing = await _db.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
        {
            return null;
        }

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedReason = "Rotated on refresh";

        var (newSession, newRefreshToken) = await StartSessionAsync(existing.User, ipAddress, userAgent, ct);
        await _db.SaveChangesAsync(ct);

        return (newSession, existing.User, newRefreshToken);
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken ct = default)
    {
        var session = await _db.UserSessions.FindAsync([sessionId], ct);
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        session.RevokedAt = DateTime.UtcNow;
        session.RevokedReason = reason;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetVariableAsync(Guid sessionId, string key, string value, CancellationToken ct = default)
    {
        var session = await _db.UserSessions.FindAsync([sessionId], ct);
        if (session is null)
        {
            return;
        }

        var data = string.IsNullOrWhiteSpace(session.DataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(session.DataJson) ?? new();

        data[key] = value;
        session.DataJson = JsonSerializer.Serialize(data);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string?> GetVariableAsync(Guid sessionId, string key, CancellationToken ct = default)
    {
        var session = await _db.UserSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session?.DataJson is null)
        {
            return null;
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(session.DataJson);
        return data is not null && data.TryGetValue(key, out var value) ? value : null;
    }
}
