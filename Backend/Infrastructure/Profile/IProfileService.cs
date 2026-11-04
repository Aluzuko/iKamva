using Backend.Models.Profile;

namespace Backend.Infrastructure.Profile;

public interface IProfileService
{
    Task<ProfileResponse?> GetProfileAsync(Guid userId, CancellationToken ct = default);

    Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
}
