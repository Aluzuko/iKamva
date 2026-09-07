using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public interface IDisputeAdminService
{
    Task<IReadOnlyList<DisputeDto>> GetDisputesAsync(CancellationToken ct = default);

    Task<bool> ResolveAsync(Guid disputeId, string resolution, CancellationToken ct = default);

    Task<bool> RejectAsync(Guid disputeId, string resolution, CancellationToken ct = default);
}

public class DisputeAdminService : IDisputeAdminService
{
    private readonly AppDbContext _db;

    public DisputeAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DisputeDto>> GetDisputesAsync(CancellationToken ct = default)
    {
        return await (
            from d in _db.Disputes
            join user in _db.Users on d.RaisedByUserId equals user.Id
            orderby d.CreatedAt descending
            select new DisputeDto
            {
                DisputeId = d.Id,
                RaisedByName = user.FullName,
                Subject = d.Subject,
                Description = d.Description,
                Status = d.Status,
                Resolution = d.Resolution,
                CreatedAt = d.CreatedAt,
                ResolvedAt = d.ResolvedAt,
            }
        ).ToListAsync(ct);
    }

    public Task<bool> ResolveAsync(Guid disputeId, string resolution, CancellationToken ct = default) =>
        SetStatusAsync(disputeId, DisputeStatus.Resolved, resolution, ct);

    public Task<bool> RejectAsync(Guid disputeId, string resolution, CancellationToken ct = default) =>
        SetStatusAsync(disputeId, DisputeStatus.Rejected, resolution, ct);

    private async Task<bool> SetStatusAsync(
        Guid disputeId, DisputeStatus status, string resolution, CancellationToken ct)
    {
        var dispute = await _db.Disputes.FindAsync([disputeId], ct);
        if (dispute is null)
        {
            return false;
        }

        dispute.Status = status;
        dispute.Resolution = resolution;
        dispute.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
