namespace DeutschMentor.Api.Common;

public static class RequestExtensions
{
    public static int GetUtcOffsetMinutes(this HttpRequest request) =>
        int.TryParse(request.Headers["X-Timezone-Offset"].ToString(), out var minutes)
            ? Math.Clamp(minutes, -840, 840)
            : 0;
}