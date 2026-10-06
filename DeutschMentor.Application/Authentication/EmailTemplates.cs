namespace DeutschMentor.Application.Authentication;

internal sealed record EmailContent(string Subject, string HtmlBody);

internal static class EmailTemplates
{
    public static EmailContent Verification(string name, string code, int minutes) => new(
        "Confirm your DeutschMentor email",
        Layout($"Hi {WebUtility.HtmlEncode(name)}, use this code to confirm your email:", code, minutes));

    public static EmailContent PasswordReset(string code, int minutes) => new(
        "Reset your DeutschMentor password",
        Layout("Use this code to reset your password:", code, minutes));

    private static string Layout(string intro, string code, int minutes) =>
        $"""
        <div style="background-color:#F4F4F4;padding:24px 12px;font-family:Arial,Helvetica,sans-serif">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
                 style="max-width:480px;margin:auto;background-color:#FFFFFF;border:2px solid #000000">
            <tr><td style="height:14px;line-height:14px;font-size:0;background-color:#000000">&nbsp;</td></tr>
            <tr><td style="height:14px;line-height:14px;font-size:0;background-color:#DD0000">&nbsp;</td></tr>
            <tr><td style="height:14px;line-height:14px;font-size:0;background-color:#FFCE00">&nbsp;</td></tr>
            <tr>
              <td style="padding:28px 28px 8px 28px">
                <div style="font-size:26px;font-weight:700;color:#000000;letter-spacing:0.5px">
                  Deutsch<span style="color:#DD0000">Mentor</span>
                </div>
                <div style="font-size:13px;color:#555555;margin-top:4px">Learn German, step by step</div>
              </td>
            </tr>
            <tr>
              <td style="padding:16px 28px 0 28px;font-size:16px;line-height:24px;color:#222222">
                {intro}
              </td>
            </tr>
            <tr>
              <td style="padding:20px 28px" align="center">
                <div style="display:inline-block;padding:14px 28px;background-color:#000000;border-bottom:6px solid #DD0000;color:#FFCE00;font-size:34px;font-weight:700;letter-spacing:10px">
                  {code}
                </div>
              </td>
            </tr>
            <tr>
              <td style="padding:0 28px 28px 28px;font-size:14px;line-height:22px;color:#555555">
                This code expires in {minutes} minutes. If you did not request it, you can ignore this email.
              </td>
            </tr>
            <tr><td style="height:14px;line-height:14px;font-size:0;background-color:#000000">&nbsp;</td></tr>
            <tr><td style="height:14px;line-height:14px;font-size:0;background-color:#DD0000">&nbsp;</td></tr>
            <tr><td style="height:14px;line-height:14px;font-size:0;background-color:#FFCE00">&nbsp;</td></tr>
          </table>
        </div>
        """;
}