using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Storage;
using Backend.Models.Tutor;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Tutors;

public interface ITutorProfileService
{
    Task<TutorProfileResponse?> GetProfileAsync(Guid tutorId, CancellationToken ct = default);

    Task<IReadOnlyList<TutorSubjectOptionDto>> GetSubjectOptionsAsync(CancellationToken ct = default);

    Task<ServiceResult> UpdateProfileAsync(Guid tutorId, UpdateTutorProfileRequest request, CancellationToken ct = default);

    Task<ServiceResult<TutorDocumentDto>> UploadDocumentAsync(Guid tutorId, DocumentType type, IFormFile file, CancellationToken ct = default);

    Task<ServiceResult> DeleteDocumentAsync(Guid tutorId, Guid documentId, CancellationToken ct = default);

    Task<ServiceResult> SubmitForReviewAsync(Guid tutorId, CancellationToken ct = default);

    Task<(StoredFile File, string FileName)?> OpenDocumentAsync(Guid documentId, Guid? ownerTutorId, CancellationToken ct = default);
}

public class TutorProfileService : ITutorProfileService
{
    public const long MaxDocumentBytes = 5 * 1024 * 1024;

    private static readonly DocumentType[] RequiredDocuments =
        [DocumentType.IdDocument, DocumentType.Qualification, DocumentType.BackgroundCheck];

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;

    public TutorProfileService(AppDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<TutorProfileResponse?> GetProfileAsync(Guid tutorId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == tutorId && u.Role == UserRole.Tutor, ct);
        if (user is null)
        {
            return null;
        }

        var profile = await LoadProfileAsync(tutorId, ct);

        return new TutorProfileResponse
        {
            FullName = user.FullName,
            Email = user.Email,
            Bio = profile?.Bio,
            HourlyRate = profile is { HourlyRate: > 0 } ? profile.HourlyRate : null,
            YearsExperience = profile?.YearsExperience ?? 0,
            SubjectIds = profile?.Subjects.Select(s => s.SubjectId).ToList() ?? new List<Guid>(),
            VerificationState = TutorRules.VerificationState(profile),
            RejectionReason = profile?.VerificationStatus == VerificationStatus.Rejected ? profile.RejectionReason : null,
            SubmittedAt = profile?.SubmittedAt,
            CanEditDocuments = TutorRules.CanEditDocuments(profile),
            Documents = profile?.Documents.OrderBy(d => d.DocumentType).Select(MapDocument).ToList() ?? new List<TutorDocumentDto>(),
            MissingForSubmission = MissingItems(profile),
        };
    }

    public async Task<IReadOnlyList<TutorSubjectOptionDto>> GetSubjectOptionsAsync(CancellationToken ct = default)
    {
        return await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new TutorSubjectOptionDto { SubjectId = s.Id, Name = s.Name, DefaultHourlyRate = s.DefaultHourlyRate })
            .ToListAsync(ct);
    }

    public async Task<ServiceResult> UpdateProfileAsync(Guid tutorId, UpdateTutorProfileRequest request, CancellationToken ct = default)
    {
        var subjectIds = request.SubjectIds.Distinct().ToList();
        if (subjectIds.Count == 0)
        {
            return ServiceResult.Invalid("Choose at least one subject.");
        }

        var knownCount = await _db.Subjects.CountAsync(s => subjectIds.Contains(s.Id), ct);
        if (knownCount != subjectIds.Count)
        {
            return ServiceResult.Invalid("One or more subjects don't exist.");
        }

        var profile = await EnsureProfileAsync(tutorId, ct);
        if (profile is null)
        {
            return ServiceResult.NotFound();
        }

        profile.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        profile.HourlyRate = request.HourlyRate;
        profile.YearsExperience = request.YearsExperience;

        var existing = profile.Subjects.ToList();
        foreach (var removed in existing.Where(s => !subjectIds.Contains(s.SubjectId)))
        {
            _db.TutorSubjects.Remove(removed);
        }
        foreach (var added in subjectIds.Where(id => existing.All(s => s.SubjectId != id)))
        {
            _db.TutorSubjects.Add(new TutorSubject { TutorUserId = tutorId, SubjectId = added });
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<TutorDocumentDto>> UploadDocumentAsync(
        Guid tutorId, DocumentType type, IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0)
        {
            return ServiceResult<TutorDocumentDto>.From(ServiceResult.Invalid("The file is empty."));
        }
        if (file.Length > MaxDocumentBytes)
        {
            return ServiceResult<TutorDocumentDto>.From(ServiceResult.Invalid("Files must be 5 MB or smaller."));
        }

        var profile = await EnsureProfileAsync(tutorId, ct);
        if (profile is null)
        {
            return ServiceResult<TutorDocumentDto>.From(ServiceResult.NotFound());
        }
        if (!TutorRules.CanEditDocuments(profile))
        {
            return ServiceResult<TutorDocumentDto>.From(ServiceResult.Conflict(LockedMessage(profile)));
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var detected = DetectFileType(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 8)));
        if (detected is null)
        {
            return ServiceResult<TutorDocumentDto>.From(ServiceResult.Invalid("Upload a PDF, JPG or PNG file."));
        }

        var document = profile.Documents.FirstOrDefault(d => d.DocumentType == type);
        var previousKey = document?.FileUrl;
        var documentId = document?.Id ?? Guid.NewGuid();
        var key = $"tutors/{tutorId}/{documentId}-{DateTime.UtcNow:yyyyMMddHHmmss}{detected.Value.Extension}";

        buffer.Position = 0;
        await _storage.SaveAsync(key, buffer, detected.Value.ContentType, ct);

        var fileName = CleanFileName(file.FileName, detected.Value.Extension);
        if (document is null)
        {
            document = new VerificationDocument
            {
                Id = documentId,
                TutorUserId = tutorId,
                DocumentType = type,
                FileName = fileName,
                FileUrl = key,
            };
            _db.VerificationDocuments.Add(document);
        }
        else
        {
            document.FileName = fileName;
            document.FileUrl = key;
        }

        document.ContentType = detected.Value.ContentType;
        document.SizeBytes = buffer.Length;
        document.UploadedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (previousKey is not null && previousKey != key && !IsExternal(previousKey))
        {
            await _storage.DeleteAsync(previousKey, ct);
        }

        return ServiceResult<TutorDocumentDto>.Ok(MapDocument(document));
    }

    public async Task<ServiceResult> DeleteDocumentAsync(Guid tutorId, Guid documentId, CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(tutorId, ct);
        var document = profile?.Documents.FirstOrDefault(d => d.Id == documentId);
        if (profile is null || document is null)
        {
            return ServiceResult.NotFound();
        }
        if (!TutorRules.CanEditDocuments(profile))
        {
            return ServiceResult.Conflict(LockedMessage(profile));
        }

        _db.VerificationDocuments.Remove(document);
        await _db.SaveChangesAsync(ct);

        if (!IsExternal(document.FileUrl))
        {
            await _storage.DeleteAsync(document.FileUrl, ct);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SubmitForReviewAsync(Guid tutorId, CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(tutorId, ct);
        if (profile is null)
        {
            return ServiceResult.Invalid("Complete your profile first.");
        }
        if (!TutorRules.CanEditDocuments(profile))
        {
            return ServiceResult.Conflict(LockedMessage(profile));
        }

        var missing = MissingItems(profile);
        if (missing.Count > 0)
        {
            return ServiceResult.Invalid("Still needed: " + string.Join(", ", missing) + ".");
        }

        profile.VerificationStatus = VerificationStatus.Pending;
        profile.RejectionReason = null;
        profile.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<(StoredFile File, string FileName)?> OpenDocumentAsync(
        Guid documentId, Guid? ownerTutorId, CancellationToken ct = default)
    {
        var document = await _db.VerificationDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null || (ownerTutorId is not null && document.TutorUserId != ownerTutorId))
        {
            return null;
        }
        if (IsExternal(document.FileUrl))
        {
            return null;
        }

        var file = await _storage.OpenReadAsync(document.FileUrl, ct);
        return file is null ? null : (file, document.FileName);
    }

    private async Task<TutorProfile?> LoadProfileAsync(Guid tutorId, CancellationToken ct) =>
        await _db.TutorProfiles
            .Include(t => t.Subjects)
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.UserId == tutorId, ct);

    private async Task<TutorProfile?> EnsureProfileAsync(Guid tutorId, CancellationToken ct)
    {
        var profile = await LoadProfileAsync(tutorId, ct);
        if (profile is not null)
        {
            return profile;
        }

        var isTutor = await _db.Users.AnyAsync(u => u.Id == tutorId && u.Role == UserRole.Tutor, ct);
        if (!isTutor)
        {
            return null;
        }

        profile = new TutorProfile { UserId = tutorId, VerificationStatus = VerificationStatus.Pending };
        _db.TutorProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return profile;
    }

    private static List<string> MissingItems(TutorProfile? profile)
    {
        var missing = new List<string>();
        if (profile is null || profile.HourlyRate <= 0)
        {
            missing.Add("hourly rate");
        }
        if (profile is null || profile.Subjects.Count == 0)
        {
            missing.Add("at least one subject");
        }
        foreach (var type in RequiredDocuments)
        {
            if (profile is null || profile.Documents.All(d => d.DocumentType != type))
            {
                missing.Add(type switch
                {
                    DocumentType.IdDocument => "ID document",
                    DocumentType.Qualification => "qualification",
                    _ => "background check",
                });
            }
        }
        return missing;
    }

    private static string LockedMessage(TutorProfile profile) =>
        profile.VerificationStatus == VerificationStatus.Approved
            ? "You're verified. Contact support to change your documents."
            : "Your documents are being reviewed. You can change them if the review asks for changes.";

    private static TutorDocumentDto MapDocument(VerificationDocument d) => new()
    {
        DocumentId = d.Id,
        DocumentType = d.DocumentType,
        FileName = d.FileName,
        SizeBytes = d.SizeBytes,
        UploadedAt = d.UploadedAt,
    };

    private static bool IsExternal(string key) =>
        key.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || key.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static (string ContentType, string Extension)? DetectFileType(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 5 && head[..5].SequenceEqual("%PDF-"u8))
        {
            return ("application/pdf", ".pdf");
        }
        if (head.Length >= 8 && head[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ("image/png", ".png");
        }
        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
        {
            return ("image/jpeg", ".jpg");
        }
        return null;
    }

    private static string CleanFileName(string original, string extension)
    {
        var name = Path.GetFileNameWithoutExtension(Path.GetFileName(original ?? string.Empty));
        name = new string(name.Where(c => !char.IsControl(c) && c is not ('/' or '\\' or '"' or ':' or '*' or '?' or '<' or '>' or '|')).ToArray()).Trim();
        if (name.Length == 0)
        {
            name = "document";
        }
        if (name.Length > 200)
        {
            name = name[..200];
        }
        return name + extension;
    }
}
