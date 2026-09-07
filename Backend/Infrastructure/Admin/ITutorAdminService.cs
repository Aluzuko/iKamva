using Backend.Models.Admin;

namespace Backend.Infrastructure.Admin;

public interface ITutorAdminService
{
    Task<IReadOnlyList<TutorListItemDto>> GetTutorsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TutorDetailDto>> GetVerificationQueueAsync(CancellationToken ct = default);

    Task<TutorDetailDto?> GetTutorAsync(Guid tutorId, CancellationToken ct = default);

    Task<Guid> CreateTutorAsync(CreateTutorRequest request, CancellationToken ct = default);

    Task<bool> ApproveTutorAsync(Guid tutorId, CancellationToken ct = default);

    Task<bool> RejectTutorAsync(Guid tutorId, string reason, CancellationToken ct = default);
}
