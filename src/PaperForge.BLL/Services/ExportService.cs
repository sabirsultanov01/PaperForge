using PaperForge.BLL.DTOs;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PaperForge.BLL.Services;

public class ExportService : IExportService
{
    private readonly IPaperRepository _paperRepo;
    private readonly IReferenceRepository _refRepo;
    private readonly ICitationService _citationService;

    public ExportService(
        IPaperRepository paperRepo,
        IReferenceRepository refRepo,
        ICitationService citationService)
    {
        _paperRepo = paperRepo;
        _refRepo = refRepo;
        _citationService = citationService;
    }

    public async Task<(byte[] FileBytes, string FileName, string ContentType)>
        ExportPaperAsync(Guid paperId, ExportOptionsDto opts)
    {
        var paper = await _paperRepo.GetWithSectionsAsync(paperId);
        if (paper is null)
            throw new InvalidOperationException("Paper not found.");

        var references = await _refRepo.GetByPaperIdAsync(paperId);
        var bibliography = _citationService.FormatBibliography(
            references, paper.CitationStyle);

        var sections = paper.Sections
            .OrderBy(s => s.OrderIndex).ToList();

        return opts.Format switch
        {
            ExportFormat.PDF  => GeneratePdf(paper, sections, bibliography, opts),
            ExportFormat.DOCX => GenerateDocx(paper, sections, bibliography, opts),
            _ => GeneratePdf(paper, sections, bibliography, opts),
        };
    }

    // ═══════════════════════════════════════════════════════
    // PDF EXPORT - APA 7 Student Paper Format
    // ═══════════════════════════════════════════════════════
    // Title page: centered, bold title 3-4 lines down, then
    //   author name, department/institution, course, instructor, date
    // No running head for student papers (APA 7 change)
    // Page numbers top-right on every page
    // Body: title repeated bold+centered on page 2, then text
    // Section headings: Level 1 = centered+bold, Level 2 = left+bold
    // References: new page, "References" centered+bold, hanging indent
    // ═══════════════════════════════════════════════════════

    private static (byte[], string, string) GeneratePdf(
        Paper paper, List<Section> sections,
        List<string> bibliography, ExportOptionsDto opts)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(1, Unit.Inch);
                page.MarginVertical(1, Unit.Inch);
                page.DefaultTextStyle(x => x
                    .FontFamily(opts.FontFamily)
                    .FontSize(opts.FontSize)
                    .LineHeight(2f));

                // APA 7 Student: page number top-right, no running head
                page.Header().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(opts.FontSize);
                });

                page.Content().Column(col =>
                {
                    // ═══ TITLE PAGE ═══
                    if (opts.IncludeTitlePage)
                    {
                        // APA 7: Title 3-4 lines from top, centered, bold, title case
                        col.Item().PaddingTop(120).AlignCenter().Column(tp =>
                        {
                            tp.Item().AlignCenter().Text(paper.Title)
                                .FontSize(opts.FontSize).Bold()
                                .FontFamily(opts.FontFamily);

                            // One blank double-spaced line, then author name
                            tp.Item().PaddingTop(24).AlignCenter().Text(
                                paper.AuthorName ?? "Student Name")
                                .FontSize(opts.FontSize);

                            // Department/Institution immediately after
                            if (!string.IsNullOrEmpty(paper.Institution))
                                tp.Item().AlignCenter().Text(paper.Institution)
                                    .FontSize(opts.FontSize);

                            // Course number and name
                            if (!string.IsNullOrEmpty(paper.CourseName))
                                tp.Item().AlignCenter().Text(paper.CourseName)
                                    .FontSize(opts.FontSize);

                            // Instructor name
                            if (!string.IsNullOrEmpty(paper.InstructorName))
                                tp.Item().AlignCenter().Text(paper.InstructorName)
                                    .FontSize(opts.FontSize);

                            // Due date
                            tp.Item().AlignCenter().Text(
                                paper.Deadline?.ToString("MMMM d, yyyy")
                                ?? DateTime.Now.ToString("MMMM d, yyyy"))
                                .FontSize(opts.FontSize);
                        });
                        col.Item().PageBreak();
                    }

                    // ═══ BODY ═══
                    // APA 7: Paper title repeated bold+centered above first paragraph
                    col.Item().AlignCenter().Text(paper.Title)
                        .FontSize(opts.FontSize).Bold();

                    var isFirstSection = true;
                    foreach (var section in sections)
                    {
                        var text = section.PlainText
                            ?? ExtractPlainText(section.Content)
                            ?? "[No content]";

                        // Determine heading level based on position/convention
                        // First section's content appears right after the bold title (no heading)
                        // Subsequent sections get Level 1 headings (centered, bold)
                        if (isFirstSection)
                        {
                            // First section: text starts as new paragraph after title
                            col.Item().PaddingTop(4).Text(
                                GetSectionText(section)).FontSize(opts.FontSize);
                            isFirstSection = false;
                        }
                        else
                        {
                            // Level 1 heading: centered, bold, title case
                            col.Item().PaddingTop(4).AlignCenter()
                                .Text(section.Title)
                                .FontSize(opts.FontSize).Bold();

                            col.Item().PaddingTop(4).Text(
                                GetSectionText(section)).FontSize(opts.FontSize);
                        }
                    }

                    // ═══ REFERENCES ═══
                    // APA 7: New page, "References" centered+bold, entries with hanging indent
                    if (bibliography.Count > 0)
                    {
                        col.Item().PageBreak();
                        col.Item().AlignCenter().Text(
                            paper.CitationStyle == CitationStyle.MLA9
                                ? "Works Cited" : "References")
                            .FontSize(opts.FontSize).Bold();

                        foreach (var entry in bibliography)
                        {
                            // Hanging indent: subsequent lines indented 0.5"
                            col.Item().PaddingTop(4).PaddingLeft(36)
                                .Text(entry).FontSize(opts.FontSize);
                        }
                    }
                });

                // No footer needed - page numbers are in header
            });
        });

        var bytes = doc.GeneratePdf();
        var filename = SanitizeFilename(paper.Title) + ".pdf";
        return (bytes, filename, "application/pdf");
    }

    private static string GetSectionText(Section section)
    {
        return section.PlainText
            ?? ExtractPlainText(section.Content)
            ?? "[No content]";
    }

    // ═══════════════════════════════════════════════════════
    // DOCX EXPORT - APA 7 Student Paper Format
    // ═══════════════════════════════════════════════════════

    private static (byte[], string, string) GenerateDocx(
        Paper paper, List<Section> sections,
        List<string> bibliography, ExportOptionsDto opts)
    {
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(
            stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
            var body = mainPart.Document.Body!;

            var fontName = opts.FontFamily;
            var fontSize = (opts.FontSize * 2).ToString(); // half-points
            var lineSpacing = "480"; // APA 7: double-spaced = 480 twips (240 * 2)

            // Add page margins (1 inch = 1440 twips)
            var sectionProps = new SectionProperties(
                new PageMargin
                {
                    Top = 1440, Bottom = 1440,
                    Left = 1440, Right = 1440,
                    Header = 720, Footer = 720
                },
                new DocumentFormat.OpenXml.Wordprocessing.PageSize { Width = 12240, Height = 15840 } // Letter size
            );

            // Add page numbers top-right (APA 7 student: no running head)
            AddPageNumbers(mainPart);

            // ═══ TITLE PAGE ═══
            if (opts.IncludeTitlePage)
            {
                // 3-4 blank lines before title
                for (var i = 0; i < 3; i++)
                    AddEmptyParagraph(body, fontName, fontSize, lineSpacing);

                // Title: centered, bold
                AddCenteredParagraph(body, paper.Title, fontName, fontSize, true, lineSpacing);

                // Blank line
                AddEmptyParagraph(body, fontName, fontSize, lineSpacing);

                // Author name: centered
                AddCenteredParagraph(body, paper.AuthorName ?? "Student Name", fontName, fontSize, false, lineSpacing);

                // Department/Institution: centered, immediately after
                if (!string.IsNullOrEmpty(paper.Institution))
                    AddCenteredParagraph(body, paper.Institution, fontName, fontSize, false, lineSpacing);

                // Course: centered
                if (!string.IsNullOrEmpty(paper.CourseName))
                    AddCenteredParagraph(body, paper.CourseName, fontName, fontSize, false, lineSpacing);

                // Instructor: centered
                if (!string.IsNullOrEmpty(paper.InstructorName))
                    AddCenteredParagraph(body, paper.InstructorName, fontName, fontSize, false, lineSpacing);

                // Due date: centered
                var dateStr = paper.Deadline?.ToString("MMMM d, yyyy")
                    ?? DateTime.Now.ToString("MMMM d, yyyy");
                AddCenteredParagraph(body, dateStr, fontName, fontSize, false, lineSpacing);

                // Page break
                body.AppendChild(new Paragraph(
                    new Run(new Break { Type = BreakValues.Page })));
            }

            // ═══ BODY ═══
            // APA 7: Paper title repeated bold+centered above first body paragraph
            AddCenteredParagraph(body, paper.Title, fontName, fontSize, true, lineSpacing);

            var isFirstSection = true;
            foreach (var section in sections)
            {
                var text = section.PlainText
                    ?? ExtractPlainText(section.Content)
                    ?? "[No content]";

                if (isFirstSection)
                {
                    // First section: body text starts directly (no "Introduction" heading)
                    AddBodyParagraphs(body, text, fontName, fontSize, lineSpacing);
                    isFirstSection = false;
                }
                else
                {
                    // Level 1 heading: centered, bold
                    AddCenteredParagraph(body, section.Title, fontName, fontSize, true, lineSpacing);
                    AddBodyParagraphs(body, text, fontName, fontSize, lineSpacing);
                }
            }

            // ═══ REFERENCES ═══
            if (bibliography.Count > 0)
            {
                body.AppendChild(new Paragraph(
                    new Run(new Break { Type = BreakValues.Page })));

                var bibTitle = paper.CitationStyle == CitationStyle.MLA9
                    ? "Works Cited" : "References";
                AddCenteredParagraph(body, bibTitle, fontName, fontSize, true, lineSpacing);

                foreach (var entry in bibliography)
                {
                    // Hanging indent: Left=720 (0.5"), Hanging=720
                    var para = new Paragraph(
                        new ParagraphProperties(
                            new Indentation { Hanging = "720", Left = "720" },
                            new SpacingBetweenLines { Line = lineSpacing, LineRule = LineSpacingRuleValues.Auto }),
                        new Run(
                            new RunProperties(
                                new RunFonts { Ascii = fontName, HighAnsi = fontName },
                                new FontSize { Val = fontSize }),
                            new Text(entry) { Space = SpaceProcessingModeValues.Preserve }));
                    body.AppendChild(para);
                }
            }

            body.AppendChild(sectionProps);
        }

        var bytes = stream.ToArray();
        var filename = SanitizeFilename(paper.Title) + ".docx";
        return (bytes, filename,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    // ═══════════════════════════════════════════════════════
    // DOCX HELPERS
    // ═══════════════════════════════════════════════════════

    private static void AddPageNumbers(MainDocumentPart mainPart)
    {
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        var headerId = mainPart.GetIdOfPart(headerPart);

        // Page number aligned right
        headerPart.Header = new Header(
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Right }),
                new Run(
                    new RunProperties(new FontSize { Val = "24" }),
                    new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(
                    new RunProperties(new FontSize { Val = "24" }),
                    new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new RunProperties(new FontSize { Val = "24" }),
                    new FieldChar { FieldCharType = FieldCharValues.End })));

        // Reference the header in section properties will be handled by sectionProps
        // We need to ensure it's referenced - add HeaderReference to body's section props
        headerPart.Header.Save();

        // Add default header reference
        var body = mainPart.Document?.Body;
        if (body is null) return;
        var existingSp = body.Elements<SectionProperties>().FirstOrDefault();
        if (existingSp is null)
        {
            existingSp = new SectionProperties();
            body.AppendChild(existingSp);
        }
        existingSp.PrependChild(new HeaderReference
        {
            Type = HeaderFooterValues.Default,
            Id = headerId
        });
    }

    private static void AddCenteredParagraph(
        Body body, string text, string font,
        string fontSize, bool bold, string lineSpacing)
    {
        var rp = new RunProperties(
            new RunFonts { Ascii = font, HighAnsi = font },
            new FontSize { Val = fontSize });
        if (bold) rp.AppendChild(new Bold());

        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { Line = lineSpacing, LineRule = LineSpacingRuleValues.Auto }),
            new Run(rp, new Text(text))));
    }

    private static void AddEmptyParagraph(Body body, string font, string fontSize, string lineSpacing)
    {
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Line = lineSpacing, LineRule = LineSpacingRuleValues.Auto }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = font, HighAnsi = font },
                    new FontSize { Val = fontSize }))));
    }

    private static void AddBodyParagraphs(
        Body body, string text, string font, string fontSize, string lineSpacing)
    {
        // APA 7: First line of each paragraph indented 0.5 inch (720 twips)
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var para = new Paragraph(
                new ParagraphProperties(
                    new Indentation { FirstLine = "720" },
                    new SpacingBetweenLines { Line = lineSpacing, LineRule = LineSpacingRuleValues.Auto }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = font, HighAnsi = font },
                        new FontSize { Val = fontSize }),
                    new Text(trimmed) { Space = SpaceProcessingModeValues.Preserve }));
            body.AppendChild(para);
        }
    }

    // ═══════════════════════════════════════════════════════
    // SHARED HELPERS
    // ═══════════════════════════════════════════════════════

    private static string? ExtractPlainText(string? deltaJson)
    {
        if (string.IsNullOrWhiteSpace(deltaJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(deltaJson);
            if (!doc.RootElement.TryGetProperty("ops", out var ops))
                return deltaJson;

            var sb = new System.Text.StringBuilder();
            foreach (var op in ops.EnumerateArray())
            {
                if (op.TryGetProperty("insert", out var insert)
                    && insert.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    sb.Append(insert.GetString());
                }
            }
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return deltaJson;
        }
    }

    private static string SanitizeFilename(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(title
            .Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return clean.Length > 100 ? clean[..100] : clean;
    }
}
