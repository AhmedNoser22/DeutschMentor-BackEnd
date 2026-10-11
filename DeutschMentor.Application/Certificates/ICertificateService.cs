namespace DeutschMentor.Application.Certificates;

public sealed record CertificateFile(byte[] Content, string FileName);

public interface ICertificatePdfGenerator
{
    byte[] Generate(CertificateDto certificate);
}

public interface ICertificateService
{
    Task<IReadOnlyList<CertificateSlotDto>> ListAsync(Guid userId, CancellationToken ct);
    Task<CertificateDto> GetAsync(Guid userId, Guid id, CancellationToken ct);
    Task<CertificateFile> GetPdfAsync(Guid userId, Guid id, CancellationToken ct);
    Task<VerifyCertificateDto> VerifyAsync(string? code, CancellationToken ct);
}