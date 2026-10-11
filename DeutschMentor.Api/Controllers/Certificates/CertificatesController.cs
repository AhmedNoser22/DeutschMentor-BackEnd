namespace DeutschMentor.Api.Certificates;

[ApiController]
[Route("api/certificates")]
public sealed class CertificatesController(ICertificateService certificates) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<CertificateSlotDto>>> List(CancellationToken ct) =>
        Ok(await certificates.ListAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<CertificateDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await certificates.GetAsync(User.GetUserId(), id, ct));

    [HttpGet("{id:guid}/pdf")]
    [Authorize]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var file = await certificates.GetPdfAsync(User.GetUserId(), id, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }

    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VerifyCertificateDto>> Verify(string code, CancellationToken ct) =>
        Ok(await certificates.VerifyAsync(code, ct));
}