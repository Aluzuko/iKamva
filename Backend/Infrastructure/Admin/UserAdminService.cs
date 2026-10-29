using Backend.Domain.Enums;
using Backend.Infrastructure.Auth;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAccountDto>> GetUsersAsync(UserRole? role, CancellationToken ct = default);

    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _sessions;

    public UserAdminService(AppDbContext db, IUserSessionService sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public async Task<IReadOnlyList<UserAccountDto>> GetUsersAsync(UserRole? role, CancellationToken ct = default)
    {
        var query = _db.Users.Where(u => u.Role == UserRole.Student || u.Role == UserRole.Parent);

        if (role is not null)
        {
            query = query.Where(u => u.Role == role);
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAccountDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null || user.Role == UserRole.Admin)
        {
            return false;
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (!isActive)
        {
            await _sessions.RevokeAllSessionsAsync(user.Id, "Account suspended", ct);
        }

        return true;
    }
}
