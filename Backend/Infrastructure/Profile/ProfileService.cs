using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Profile;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Profile;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileResponse?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return null;
        }

        var response = new ProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            FullName = user.FullName,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
        };

        return user.Role switch
        {
            UserRole.Student => await AttachStudentDataAsync(response, user.Id, ct),
            UserRole.Tutor => await AttachTutorDataAsync(response, user.Id, ct),
            _ => response,
        };
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return false;
        }

        user.FullName = request.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        if (user.Role == UserRole.Tutor)
        {
            var tutorProfile = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
            if (tutorProfile is not null)
            {
                if (request.Bio is not null)
                {
                    tutorProfile.Bio = request.Bio;
                }
                if (request.HourlyRate is not null)
                {
                    tutorProfile.HourlyRate = request.HourlyRate.Value;
                }
            }
        }

        if (user.Role == UserRole.Student && request.YearLevel is not null)
        {
            var studentProfile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId, ct);
            if (studentProfile is not null)
            {
                studentProfile.YearLevel = request.YearLevel;
            }
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<ProfileResponse> AttachStudentDataAsync(ProfileResponse response, Guid userId, CancellationToken ct)
    {
        var profile = await _db.StudentProfiles
            .Include(p => p.Subjects).ThenInclude(s => s.Subject)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
        {
            return response;
        }

        return new ProfileResponse
        {
            UserId = response.UserId,
            Email = response.Email,
            IsEmailVerified = response.IsEmailVerified,
            FullName = response.FullName,
            Role = response.Role,
            CreatedAt = response.CreatedAt,
            YearLevel = profile.YearLevel,
            LinkCode = profile.LinkCode,
            EnrolledSubjects = profile.Subjects.Select(s => s.Subject.Name).ToList(),
        };
    }

    private async Task<ProfileResponse> AttachTutorDataAsync(ProfileResponse response, Guid userId, CancellationToken ct)
    {
        var profile = await _db.TutorProfiles
            .Include(p => p.Subjects).ThenInclude(s => s.Subject)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
        {
            return response;
        }

        return new ProfileResponse
        {
            UserId = response.UserId,
            Email = response.Email,
            IsEmailVerified = response.IsEmailVerified,
            FullName = response.FullName,
            Role = response.Role,
            CreatedAt = response.CreatedAt,
            Bio = profile.Bio,
            HourlyRate = profile.HourlyRate,
            YearsExperience = profile.YearsExperience,
            VerificationStatus = profile.VerificationStatus,
            TutorSubjects = profile.Subjects.Select(s => s.Subject.Name).ToList(),
        };
    }
}
