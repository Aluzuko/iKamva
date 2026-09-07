using Backend.Infrastructure.Admin;
using Backend.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAnalyticsAdminService _analytics;

    public AdminAnalyticsController(IAnalyticsAdminService analytics)
    {
        _analytics = analytics;
    }

    [HttpGet]
    public async Task<ActionResult<AnalyticsResponse>> GetAnalytics(CancellationToken ct)
    {
        return Ok(await _analytics.GetAnalyticsAsync(ct));
    }
}
