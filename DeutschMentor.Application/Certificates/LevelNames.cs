namespace DeutschMentor.Application.Certificates;

internal static class LevelNames
{
    public static string German(CefrLevel level) => level switch
    {
        CefrLevel.A1 => "Anfänger",
        CefrLevel.A2 => "Grundstufe",
        CefrLevel.B1 => "Mittelstufe",
        CefrLevel.B2 => "Obere Mittelstufe",
        CefrLevel.C1 => "Fortgeschrittene",
        _ => level.ToString()
    };
}