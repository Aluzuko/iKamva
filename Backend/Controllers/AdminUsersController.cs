using Backend.Domain.Enums;
using Backend.Infrastructure.Admin;
using Backend.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _userAdmin;

    public AdminUsersController(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAccountDto>>> GetUsers(
        [FromQuery] UserRole? role, CancellationToken ct)
    {
        return Ok(await _userAdmin.GetUsersAsync(role, ct));
    }

    [HttpPost("{userId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken ct)
    {
        var success = await _userAdmin.SetActiveAsync(userId, true, ct);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{userId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid userId, CancellationToken ct)
    {
        var success = await _userAdmin.SetActiveAsync(userId, false, ct);
        return success ? NoContent() : NotFound();
    }
}
