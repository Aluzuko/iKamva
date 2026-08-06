using Backend.Domain.Enums;
using Backend.Models.Parent;

namespace Backend.Infrastructure.Parents;

public interface IParentService
{
    Task<FamilyDashboardResponse> GetDashboardAsync(Guid parentUserId, CancellationToken ct = default);

    Task<IReadOnlyList<ChildSummaryDto>> GetChildrenAsync(Guid parentUserId, CancellationToken ct = default);

    Task<StudentDetailResponse?> GetChildDetailAsync(Guid parentUserId, Guid studentUserId, CancellationToken ct = default);

    Task<PaymentsOverviewResponse> GetPaymentsAsync(Guid parentUserId, CancellationToken ct = default);

    Task<bool> PayInvoiceAsync(Guid parentUserId, Guid invoiceId, CancellationToken ct = default);

    Task InviteStudentAsync(Guid parentUserId, string email, Relationship relationship, CancellationToken ct = default);

    Task<bool> LinkByCodeAsync(Guid parentUserId, string code, CancellationToken ct = default);
}