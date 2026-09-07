using Backend.Models.Admin;

namespace Backend.Infrastructure.Admin;

public interface IAdminService
{
    Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken ct = default);
}
