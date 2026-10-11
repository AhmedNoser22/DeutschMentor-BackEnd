namespace DeutschMentor.Infrastructure.Certificates;

public sealed class CertificatePdfGenerator : ICertificatePdfGenerator
{
    private const string Ink = "#14171C";
    private const string Muted = "#5D6673";
    private const string Red = "#C8231C";
    private const string Gold = "#E8B000";
    private const string ArabicFamily = "Noto Sans Arabic";

    private static readonly bool ArabicFontLoaded = RegisterFonts();

    public byte[] Generate(CertificateDto certificate)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var nameFont = ArabicFontLoaded && ContainsArabic(certificate.RecipientName) ? ArabicFamily : null;
        var issued = certificate.IssuedAtUtc.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var statement =
            $"has passed the DeutschMentor Level {certificate.LevelCode} exam, {certificate.GermanLevelName}, " +
            $"with a score of {certificate.ScorePercent}%, and has completed all lessons of the level.";

        return Document
            .Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style.FontSize(12).FontColor(Ink));

                page.Background()
                    .Padding(22).Border(3).BorderColor(Ink)
                    .Padding(6).Border(1).BorderColor(Gold)
                    .ExtendHorizontal().ExtendVertical();

                page.Header().PaddingHorizontal(60).PaddingTop(52).Row(row =>
                {
                    row.RelativeItem().Row(brand =>
                    {
                        brand.ConstantItem(40).Column(bars =>
                        {
                            bars.Item().Width(18).Height(10).Background(Ink);
                            bars.Item().PaddingTop(3).Width(28).Height(10).Background(Red);
                            bars.Item().PaddingTop(3).Width(40).Height(10).Background(Gold);
                        });

                        brand.AutoItem().PaddingLeft(10).AlignMiddle().Text("DeutschMentor").FontSize(22).Bold();
                    });

                    row.AutoItem().AlignMiddle()
                        .Text($"LEVEL {certificate.LevelCode} · CEFR").FontSize(10).LetterSpacing(0.15f).FontColor(Muted);
                });

                page.Content().PaddingHorizontal(60).PaddingTop(36).Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Text("CERTIFICATE OF ACHIEVEMENT").FontSize(10).LetterSpacing(0.2f).FontColor(Muted);
                    column.Item().Text("Zertifikat").FontSize(66).Bold();
                    column.Item().PaddingTop(10).Text("THIS CERTIFIES THAT").FontSize(10).LetterSpacing(0.2f).FontColor(Muted);
                    column.Item().Element(container => NameText(container, certificate.RecipientName, nameFont));
                    column.Item().Height(1.5f).Background(Ink);
                    column.Item().PaddingTop(10).Text(statement).FontSize(15).LineHeight(1.4f);
                });

                page.Footer().PaddingHorizontal(60).PaddingBottom(52).Row(row =>
                {
                    row.RelativeItem().AlignBottom().Column(column =>
                    {
                        column.Item().Text("ISSUED").FontSize(9).LetterSpacing(0.2f).FontColor(Muted);
                        column.Item().Text(issued).FontSize(16).Bold();
                        column.Item().PaddingTop(8).Text("CERTIFICATE ID").FontSize(9).LetterSpacing(0.2f).FontColor(Muted);
                        column.Item().Text(certificate.Code).FontSize(16).Bold();
                    });

                    row.ConstantItem(112).Height(112).Background(Gold).CornerRadius(56)
                        .AlignCenter().AlignMiddle().Column(column =>
                        {
                            column.Item().AlignCenter().Text(certificate.LevelCode).FontSize(36).Bold();
                            column.Item().AlignCenter().Text("BESTANDEN").FontSize(8).Bold().LetterSpacing(0.15f);
                        });
                });
            }))
            .GeneratePdf();
    }

    private static void NameText(IContainer container, string name, string? family)
    {
        var text = container.Text(name).FontSize(38).Bold();
        if (family is not null)
        {
            text.FontFamily(family);
        }
    }

    private static bool ContainsArabic(string value) => value.Any(ch => ch is >= '\u0600' and <= '\u06FF');

    private static bool RegisterFonts()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        if (!Directory.Exists(directory))
        {
            return false;
        }

        var found = false;
        foreach (var file in Directory.EnumerateFiles(directory, "*.ttf"))
        {
            FontManager.RegisterFont(File.OpenRead(file));
            found = true;
        }

        return found;
    }
}