namespace DeutschMentor.Application.Progress;

internal static class StreakCalculator
{
    public static StreakDto Calculate(IReadOnlySet<DateOnly> days, DateOnly today)
    {
        var studiedToday = days.Contains(today);
        var cursor = studiedToday ? today : today.AddDays(-1);

        var current = 0;
        while (days.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        var longest = 0;
        var run = 0;
        DateOnly? previous = null;

        foreach (var day in days.Order())
        {
            run = previous is { } p && day == p.AddDays(1) ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = day;
        }

        return new StreakDto(current, Math.Max(longest, current), studiedToday);
    }
}