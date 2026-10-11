namespace DeutschMentor.Application.Progress;

internal sealed record GermanDay(string Name, string Note);

internal static class GermanDays
{
    private static readonly Dictionary<(int Month, int Day), GermanDay> Days = new()
    {
        [(1, 1)] = new("Neujahr", "1 January is a public holiday in Germany, so shops and offices are closed."),
        [(5, 1)] = new("Tag der Arbeit", "1 May is a public holiday in Germany, so most shops are closed."),
        [(10, 3)] = new("Tag der Deutschen Einheit", "3 October is a public holiday in Germany, so most shops are closed."),
        [(12, 24)] = new("Heiligabend", "Many shops in Germany close early on 24 December."),
        [(12, 25)] = new("der erste Weihnachtstag", "25 December is a public holiday in Germany, so shops are closed."),
        [(12, 26)] = new("der zweite Weihnachtstag", "26 December is a public holiday in Germany, so shops are closed.")
    };

    public static GermanDay? For(DateOnly date) => Days.GetValueOrDefault((date.Month, date.Day));
}