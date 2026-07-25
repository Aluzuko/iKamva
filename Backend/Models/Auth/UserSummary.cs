using Backend.Domain.Entities;
using Backend.Domain.Enums;

namespace Backend.Models.Auth;

public sealed class UserSummary
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string FullName { get; init; }

    public required UserRole Role { get; init; }

    public static UserSummary FromEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
    };
}
