namespace DeutschMentor.Api.Authentication;

[ApiController]
[Authorize]
[Route("api/account")]
[EnableRateLimiting("auth")]
public sealed class AccountController(IAccountService account) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await account.GetMeAsync(User.GetUserId(), ct));

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await account.UpdateProfileAsync(User.GetUserId(), request, ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await account.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}