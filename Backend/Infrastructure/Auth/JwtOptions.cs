namespace Backend.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Signing secret. Must NOT live in appsettings.json — set via
    /// `dotnet user-secrets` locally or an environment variable in
    /// deployment (see README-BACKEND-SETUP.md).
    /// </summary>
    public required string Secret { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 30;
}
