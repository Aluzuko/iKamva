using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Tutor;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Tutors;

public interface ITutorAvailabilityService
{
    Task<AvailabilityResponse> GetAsync(Guid tutorId, CancellationToken ct = default);

    Task<ServiceResult> ReplaceAsync(Guid tutorId, UpdateAvailabilityRequest request, CancellationToken ct = default);
}

public class TutorAvailabilityService : ITutorAvailabilityService
{
    private const int Step = 15;
    private const int MinimumLength = 30;

    private readonly AppDbContext _db;

    public TutorAvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AvailabilityResponse> GetAsync(Guid tutorId, CancellationToken ct = default)
    {
        var slots = await _db.TutorAvailabilitySlots
            .Where(s => s.TutorUserId == tutorId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartMinute)
            .Select(s => new AvailabilitySlotDto { DayOfWeek = (int)s.DayOfWeek, StartMinute = s.StartMinute, EndMinute = s.EndMinute })
            .ToListAsync(ct);

        return new AvailabilityResponse { TimeZone = "Africa/Johannesburg", Slots = slots };
    }

    public async Task<ServiceResult> ReplaceAsync(Guid tutorId, UpdateAvailabilityRequest request, CancellationToken ct = default)
    {
        foreach (var slot in request.Slots)
        {
            if (slot.DayOfWeek is < 0 or > 6)
            {
                return ServiceResult.Invalid("Each time block needs a valid day.");
            }
            if (slot.StartMinute < 0 || slot.EndMinute > 1440 || slot.StartMinute % Step != 0 || slot.EndMinute % Step != 0)
            {
                return ServiceResult.Invalid("Times must be on the quarter hour.");
            }
            if (slot.EndMinute - slot.StartMinute < MinimumLength)
            {
                return ServiceResult.Invalid("Each time block must be at least 30 minutes long and end after it starts.");
            }
        }

        foreach (var day in request.Slots.GroupBy(s => s.DayOfWeek))
        {
            var ordered = day.OrderBy(s => s.StartMinute).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartMinute < ordered[i - 1].EndMinute)
                {
                    return ServiceResult.Invalid($"Time blocks on {(DayOfWeek)day.Key} overlap.");
                }
            }
        }

        var isTutor = await _db.Users.AnyAsync(u => u.Id == tutorId && u.Role == UserRole.Tutor, ct);
        if (!isTutor)
        {
            return ServiceResult.NotFound();
        }

        if (!await _db.TutorProfiles.AnyAsync(t => t.UserId == tutorId, ct))
        {
            _db.TutorProfiles.Add(new TutorProfile { UserId = tutorId, VerificationStatus = VerificationStatus.Pending });
        }

        var existing = await _db.TutorAvailabilitySlots.Where(s => s.TutorUserId == tutorId).ToListAsync(ct);
        _db.TutorAvailabilitySlots.RemoveRange(existing);
        _db.TutorAvailabilitySlots.AddRange(request.Slots.Select(s => new TutorAvailabilitySlot
        {
            TutorUserId = tutorId,
            DayOfWeek = (DayOfWeek)s.DayOfWeek,
            StartMinute = s.StartMinute,
            EndMinute = s.EndMinute,
        }));

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
