using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stored lower-cased. Doubles as the username — there is no separate username field.</summary>
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string FullName { get; set; }

    public UserRole Role { get; set; }

    public bool IsEmailVerified { get; set; }

    /// <summary>Admin-controlled enable/disable flag (see UserAdminService.SetActiveAsync). New accounts start active.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
