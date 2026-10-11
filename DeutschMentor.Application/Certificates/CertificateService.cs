namespace DeutschMentor.Application.Certificates;

public sealed class CertificateService(
    IApplicationDbContext db,
    ILearningService learning,
    ICertificatePdfGenerator pdf) : ICertificateService
{
    public async Task<IReadOnlyList<CertificateSlotDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var levels = await db.Levels.AsNoTracking().OrderBy(l => l.Order).ToListAsync(ct);
        var certificates = await db.Certificates.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(ct);
        var passedLevelIds = (await db.LevelPasses.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.LevelId)
            .ToListAsync(ct)).ToHashSet();
        var summaries = (await learning.GetLevelsAsync(userId, ct)).ToDictionary(s => s.Code);

        var slots = new List<CertificateSlotDto>();
        foreach (var level in levels)
        {
            var code = level.Code.ToString();
            var certificate = certificates.FirstOrDefault(c => c.LevelId == level.Id);

            CertificateSlotStatus status;
            CertificateDto? dto = null;

            if (certificate is not null)
            {
                status = CertificateSlotStatus.Issued;
                dto = ToDto(certificate, level);
            }
            else if (passedLevelIds.Contains(level.Id))
            {
                status = CertificateSlotStatus.PlacementOnly;
            }
            else if (summaries[code].Status == LevelStatus.Locked)
            {
                status = CertificateSlotStatus.Locked;
            }
            else
            {
                status = CertificateSlotStatus.NotPassed;
            }

            slots.Add(new CertificateSlotDto(code, level.Title, status, dto));
        }

        return slots;
    }

    public async Task<CertificateDto> GetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var certificate = await db.Certificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct)
            ?? throw new NotFoundException("certificate_not_found", "Certificate not found.");

        var level = await db.Levels.AsNoTracking().FirstAsync(l => l.Id == certificate.LevelId, ct);
        return ToDto(certificate, level);
    }

    public async Task<CertificateFile> GetPdfAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var certificate = await GetAsync(userId, id, ct);
        var content = pdf.Generate(certificate);
        return new CertificateFile(content, $"DeutschMentor-{certificate.LevelCode}-Zertifikat.pdf");
    }

    public async Task<VerifyCertificateDto> VerifyAsync(string? code, CancellationToken ct)
    {
        var normalized = CertificateIssuer.Normalize(code);
        if (normalized.Length is < 8 or > 40)
        {
            return VerifyCertificateDto.Invalid;
        }

        var certificate = await db.Certificates.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized, ct);
        if (certificate is null)
        {
            return VerifyCertificateDto.Invalid;
        }

        var level = await db.Levels.AsNoTracking().FirstAsync(l => l.Id == certificate.LevelId, ct);

        return new VerifyCertificateDto(
            true,
            certificate.Code,
            certificate.RecipientName,
            level.Code.ToString(),
            level.Title,
            LevelNames.German(level.Code),
            certificate.ScorePercent,
            certificate.IssuedAtUtc.AsUtc());
    }

    private static CertificateDto ToDto(Certificate certificate, Level level) => new(
        certificate.Id,
        certificate.Code,
        level.Code.ToString(),
        level.Title,
        LevelNames.German(level.Code),
        certificate.RecipientName,
        certificate.ScorePercent,
        certificate.IssuedAtUtc.AsUtc());
}