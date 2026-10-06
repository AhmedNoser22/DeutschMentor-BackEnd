namespace DeutschMentor.Infrastructure.Identity;

public sealed class CodeService(IOptions<SecurityOptions> options) : ICodeService
{
    public string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string Hash(string email, string code)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.CodePepper);
        var data = Encoding.UTF8.GetBytes($"{email}:{code.Trim()}");
        return Convert.ToHexString(HMACSHA256.HashData(key, data));
    }

    public bool Verify(string email, string code, string hash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(email, code)),
            Encoding.UTF8.GetBytes(hash));
}