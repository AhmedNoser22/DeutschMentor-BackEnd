namespace DeutschMentor.Application.Common;

internal static class LocalDay
{
    public static int ClampOffset(int minutes) => Math.Clamp(minutes, -840, 840);

    public static DateTime StartUtc(DateTime nowUtc, int offsetMinutes) =>
        nowUtc.AddMinutes(offsetMinutes).Date.AddMinutes(-offsetMinutes);

    public static DateTime EndUtc(DateTime nowUtc, int offsetMinutes) => StartUtc(nowUtc, offsetMinutes).AddDays(1);

    public static DateOnly Today(DateTime nowUtc, int offsetMinutes) =>
        DateOnly.FromDateTime(nowUtc.AddMinutes(offsetMinutes));
}