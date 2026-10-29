namespace Backend.Domain.Entities;

/// <summary>
/// Represents one logged-in session (one device/browser). The JWT access
/// token itself is stateless and never touches the database, but every
/// access token is issued alongside a session row here, identified by a
/// refresh token. This is what lets us revoke a single device, sign out
/// everywhere, and attach arbitrary session-scoped variables server-side —
/// things a bare JWT can't do on its own.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 hash of the refresh token. The raw token is never stored.</summary>
    public required string RefreshTokenHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>
    /// Arbitrary session-scoped key/value data (e.g. active role context,
    /// feature flags, last-viewed dashboard) stored as a jsonb column.
    /// Read/write via IUserSessionService rather than touching this directly.
    /// </summary>
    public string? DataJson { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
