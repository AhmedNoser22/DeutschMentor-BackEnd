namespace DeutschMentor.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(IAdminUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> List(
        [FromQuery] string? search, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int page, CancellationToken ct) =>
        Ok(await users.GetUsersAsync(search, role, status, page, ct));

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, CancellationToken ct)
    {
        await users.SetActiveAsync(id, false, User.GetUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        await users.SetActiveAsync(id, true, User.GetUserId(), ct);
        return NoContent();
    }
}