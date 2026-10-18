namespace DeutschMentor.Infrastructure.Email;

public sealed class BrevoOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "DeutschMentor";
}

public sealed class BrevoEmailSender(IOptions<BrevoOptions> options) : IEmailSender
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var brevo = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", brevo.ApiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { name = brevo.FromName, email = brevo.FromAddress },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody
        });

        using var response = await Http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Brevo rejected the email ({(int)response.StatusCode}): {body}");
        }
    }
}