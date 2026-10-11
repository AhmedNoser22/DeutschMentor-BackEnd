namespace DeutschMentor.Application.Common;

internal static class DateTimeExtensions
{
    public static DateTime AsUtc(this DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? AsUtc(this DateTime? value) => value is { } v ? v.AsUtc() : null;
}