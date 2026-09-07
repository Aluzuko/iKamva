using Backend.Domain.Entities;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public interface ISubjectAdminService
{
    Task<IReadOnlyList<SubjectPricingDto>> GetSubjectsAsync(CancellationToken ct = default);

    Task<bool> UpdatePricingAsync(Guid subjectId, UpdateSubjectPricingRequest request, CancellationToken ct = default);

    Task<Guid> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default);
}

public class SubjectAdminService : ISubjectAdminService
{
    private readonly AppDbContext _db;

    public SubjectAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubjectPricingDto>> GetSubjectsAsync(CancellationToken ct = default)
    {
        var subjects = await _db.Subjects.ToListAsync(ct);
        var result = new List<SubjectPricingDto>();

        foreach (var subject in subjects)
        {
            var tutorCount = await _db.TutorSubjects.CountAsync(t => t.SubjectId == subject.Id, ct);

            result.Add(new SubjectPricingDto
            {
                SubjectId = subject.Id,
                Name = subject.Name,
                DefaultHourlyRate = subject.DefaultHourlyRate,
                CommissionPercent = subject.CommissionPercent,
                TutorCount = tutorCount,
            });
        }

        return result;
    }

    public async Task<bool> UpdatePricingAsync(
        Guid subjectId, UpdateSubjectPricingRequest request, CancellationToken ct = default)
    {
        var subject = await _db.Subjects.FindAsync([subjectId], ct);
        if (subject is null)
        {
            return false;
        }

        subject.DefaultHourlyRate = request.DefaultHourlyRate;
        subject.CommissionPercent = request.CommissionPercent;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Guid> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        var subject = new Subject
        {
            Name = request.Name.Trim(),
            DefaultHourlyRate = request.DefaultHourlyRate,
            CommissionPercent = request.CommissionPercent,
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(ct);
        return subject.Id;
    }
}
