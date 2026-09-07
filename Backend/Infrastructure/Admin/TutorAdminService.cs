using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Auth;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public class TutorAdminService : ITutorAdminService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public TutorAdminService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<TutorListItemDto>> GetTutorsAsync(CancellationToken ct = default)
    {
        var tutors = await _db.Users.Where(u => u.Role == UserRole.Tutor).ToListAsync(ct);
        var profiles = await _db.TutorProfiles
            .Include(t => t.Subjects).ThenInclude(s => s.Subject)
            .ToListAsync(ct);

        var result = new List<TutorListItemDto>();
        foreach (var tutor in tutors)
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == tutor.Id);
            var completedCount = await _db.Bookings.CountAsync(
                b => b.TutorUserId == tutor.Id && b.Status == BookingStatus.Completed, ct);

            result.Add(new TutorListItemDto
            {
                TutorId = tutor.Id,
                FullName = tutor.FullName,
                Email = tutor.Email,
                HourlyRate = profile?.HourlyRate ?? 0m,
                YearsExperience = profile?.YearsExperience ?? 0,
                VerificationStatus = profile?.VerificationStatus ?? VerificationStatus.Pending,
                IsActive = tutor.IsActive,
                Subjects = profile?.Subjects.Select(s => s.Subject.Name).ToList() ?? new List<string>(),
                CompletedBookings = completedCount,
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<TutorDetailDto>> GetVerificationQueueAsync(CancellationToken ct = default)
    {
        var pendingProfiles = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.Subjects).ThenInclude(s => s.Subject)
            .Include(t => t.Documents)
            .Where(t => t.VerificationStatus == VerificationStatus.Pending)
            .ToListAsync(ct);

        return pendingProfiles.Select(MapDetail).ToList();
    }

    public async Task<TutorDetailDto?> GetTutorAsync(Guid tutorId, CancellationToken ct = default)
    {
        var profile = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.Subjects).ThenInclude(s => s.Subject)
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.UserId == tutorId, ct);

        return profile is null ? null : MapDetail(profile);
    }

    public async Task<Guid> CreateTutorAsync(CreateTutorRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Role = UserRole.Tutor,
            IsEmailVerified = true,
        };

        _db.Users.Add(user);

        var profile = new TutorProfile
        {
            UserId = user.Id,
            Bio = request.Bio,
            HourlyRate = request.HourlyRate,
            YearsExperience = request.YearsExperience,
            VerificationStatus = VerificationStatus.Approved,
        };

        _db.TutorProfiles.Add(profile);

        foreach (var subjectId in request.SubjectIds)
        {
            _db.TutorSubjects.Add(new TutorSubject { TutorUserId = user.Id, SubjectId = subjectId });
        }

        await _db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task<bool> ApproveTutorAsync(Guid tutorId, CancellationToken ct = default)
    {
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == tutorId, ct);
        if (profile is null)
        {
            return false;
        }

        profile.VerificationStatus = VerificationStatus.Approved;
        profile.RejectionReason = null;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectTutorAsync(Guid tutorId, string reason, CancellationToken ct = default)
    {
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == tutorId, ct);
        if (profile is null)
        {
            return false;
        }

        profile.VerificationStatus = VerificationStatus.Rejected;
        profile.RejectionReason = reason;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static TutorDetailDto MapDetail(TutorProfile profile) => new()
    {
        TutorId = profile.UserId,
        FullName = profile.User.FullName,
        Email = profile.User.Email,
        Bio = profile.Bio,
        HourlyRate = profile.HourlyRate,
        YearsExperience = profile.YearsExperience,
        VerificationStatus = profile.VerificationStatus,
        RejectionReason = profile.RejectionReason,
        Subjects = profile.Subjects.Select(s => s.Subject.Name).ToList(),
        Documents = profile.Documents.Select(d => new VerificationDocumentDto
        {
            DocumentId = d.Id,
            DocumentType = d.DocumentType,
            FileName = d.FileName,
            FileUrl = d.FileUrl,
            UploadedAt = d.UploadedAt,
        }).ToList(),
    };
}
