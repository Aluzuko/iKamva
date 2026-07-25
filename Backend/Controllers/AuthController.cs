using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Infrastructure.Auth;
using Backend.Infrastructure.Data;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserSessionService _sessions;

    public AuthController(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUserSessionService sessions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _sessions = sessions;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Role = request.Role,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await IssueAuthResponseAsync(user, ct);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new {status = 401,  message = "Invalid email or password." });
        }
        if (user.IsEmailVerified == false)
        {
            return Unauthorized(new {status = 401, message = "Email not verified, Please check your email for a verification link." });
        }

        return await IssueAuthResponseAsync(user, ct);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await _sessions.RotateRefreshTokenAsync(
            request.RefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct);

        if (result is null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        var (session, user, refreshToken) = result.Value;
        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = session.ExpiresAt,
            User = UserSummary.FromEntity(user),
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

        if (session is not null)
        {
            await _sessions.RevokeSessionAsync(session.Id, "User logged out", ct);
        }

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        await _sessions.RevokeAllSessionsAsync(GetCurrentUserId(), "User signed out of all devices", ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserSummary>> Me(CancellationToken ct)
    {
        var user = await _db.Users.FindAsync([GetCurrentUserId()], ct);
        return user is null ? NotFound() : Ok(UserSummary.FromEntity(user));
    }

    private async Task<ActionResult<AuthResponse>> IssueAuthResponseAsync(User user, CancellationToken ct)
    {
        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var (session, refreshToken) = await _sessions.StartSessionAsync(
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = session.ExpiresAt,
            User = UserSummary.FromEntity(user),
        });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.Parse(sub!);
    }
}
