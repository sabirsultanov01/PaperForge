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
    // PDF EXPORT - APA 7 (Student & Professional formats)
    // ═══════════════════════════════════════════════════════
    // Student: No running head, title page with course/instructor/date
    // Professional: Running head (shortened title ALL CAPS), author note
    // Both: Page numbers top-right, 1" margins, double-spaced, 12pt font
    // ═══════════════════════════════════════════════════════

    private static (byte[], string, string) GeneratePdf(
        Paper paper, List<Section> sections,
        List<string> bibliography, ExportOptionsDto opts)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var runningHead = GetRunningHead(paper.Title);

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

                // Header: Professional = running head left + page number right
                //         Student = page number right only
                page.Header().Row(row =>
                {
                    if (!opts.IsStudentPaper)
                    {
                        row.RelativeItem().AlignLeft().Text(runningHead)
                            .FontSize(opts.FontSize);
                    }
                    else
                    {
                        row.RelativeItem();
                    }
                    row.AutoItem().AlignRight().Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(opts.FontSize);
                    });
                });

                page.Content().Column(col =>
                {
                    // ═══ TITLE PAGE ═══
                    if (opts.IncludeTitlePage)
                    {
                        col.Item().PaddingTop(120).AlignCenter().Column(tp =>
                        {
                            // Title: centered, bold
                            tp.Item().AlignCenter().Text(paper.Title)
                                .FontSize(opts.FontSize).Bold()
                                .FontFamily(opts.FontFamily);

                            tp.Item().PaddingTop(24).AlignCenter().Text(
                                paper.AuthorName ?? "Author Name")
                                .FontSize(opts.FontSize);

                            if (!string.IsNullOrEmpty(paper.Institution))
                                tp.Item().AlignCenter().Text(paper.Institution)
                                    .FontSize(opts.FontSize);

                            if (opts.IsStudentPaper)
                            {
                                // Student: course, instructor, due date
                                if (!string.IsNullOrEmpty(paper.CourseName))
                                    tp.Item().AlignCenter().Text(paper.CourseName)
                                        .FontSize(opts.FontSize);

                                if (!string.IsNullOrEmpty(paper.InstructorName))
                                    tp.Item().AlignCenter().Text(paper.InstructorName)
                                        .FontSize(opts.FontSize);

                                tp.Item().AlignCenter().Text(
                                    paper.Deadline?.ToString("MMMM d, yyyy")
                                    ?? DateTime.Now.ToString("MMMM d, yyyy"))
                                    .FontSize(opts.FontSize);
                            }
                            else
                            {
                                // Professional: Author Note section
                                tp.Item().PaddingTop(48).AlignCenter()
                                    .Text("Author Note").FontSize(opts.FontSize).Bold();

                                tp.Item().PaddingTop(4).PaddingLeft(36)
                                    .Text($"{paper.AuthorName ?? "Author"}, {paper.Institution ?? "Institution"}.")
                                    .FontSize(opts.FontSize);

                                tp.Item().PaddingTop(4).PaddingLeft(36)
                                    .Text("Correspondence concerning this paper should be addressed to "
                                        + $"{paper.AuthorName ?? "the author"}.")
                                    .FontSize(opts.FontSize);
                            }
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
                        var text = GetSectionText(section);

                        if (isFirstSection)
                        {
                            AddPdfParagraphs(col, text, opts);
                            isFirstSection = false;
                        }
                        else
                        {
                            // Level 1 heading: centered, bold
                            col.Item().PaddingTop(4).AlignCenter()
                                .Text(section.Title)
                                .FontSize(opts.FontSize).Bold();

                            AddPdfParagraphs(col, text, opts);
                        }
                    }

                    // ═══ REFERENCES ═══
                    if (bibliography.Count > 0)
                    {
                        col.Item().PageBreak();
                        col.Item().AlignCenter().Text(
                            paper.CitationStyle == CitationStyle.MLA9
                                ? "Works Cited" : "References")
                            .FontSize(opts.FontSize).Bold();

                        foreach (var entry in bibliography)
                        {
                            col.Item().PaddingTop(4).PaddingLeft(36)
                                .Text(entry).FontSize(opts.FontSize);
                        }
                    }
                });
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

    /// <summary>
    /// Splits text into paragraphs and adds them to the PDF column with proper APA 7 formatting.
    /// </summary>
    private static void AddPdfParagraphs(
        QuestPDF.Fluent.ColumnDescriptor col, string text, ExportOptionsDto opts)
    {
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // APA 7: first-line indent of 0.5 inch (36 pt)
            col.Item().PaddingTop(2).PaddingLeft(36)
                .Text(trimmed).FontSize(opts.FontSize);
        }
    }

    // ═══════════════════════════════════════════════════════
    // DOCX EXPORT - APA 7 (Student & Professional)
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
                new DocumentFormat.OpenXml.Wordprocessing.PageSize { Width = 12240, Height = 15840 }
            );

            // Header: Professional includes running head, Student has page numbers only
            if (opts.IsStudentPaper)
            {
                AddPageNumbers(mainPart);
            }
            else
            {
                AddRunningHeadWithPageNumbers(mainPart, GetRunningHead(paper.Title), fontName, fontSize);
            }

            // ═══ TITLE PAGE ═══
            if (opts.IncludeTitlePage)
            {
                for (var i = 0; i < 3; i++)
                    AddEmptyParagraph(body, fontName, fontSize, lineSpacing);

                AddCenteredParagraph(body, paper.Title, fontName, fontSize, true, lineSpacing);
                AddEmptyParagraph(body, fontName, fontSize, lineSpacing);
                AddCenteredParagraph(body, paper.AuthorName ?? "Author Name", fontName, fontSize, false, lineSpacing);

                if (!string.IsNullOrEmpty(paper.Institution))
                    AddCenteredParagraph(body, paper.Institution, fontName, fontSize, false, lineSpacing);

                if (opts.IsStudentPaper)
                {
                    // Student: course, instructor, due date
                    if (!string.IsNullOrEmpty(paper.CourseName))
                        AddCenteredParagraph(body, paper.CourseName, fontName, fontSize, false, lineSpacing);
                    if (!string.IsNullOrEmpty(paper.InstructorName))
                        AddCenteredParagraph(body, paper.InstructorName, fontName, fontSize, false, lineSpacing);

                    var dateStr = paper.Deadline?.ToString("MMMM d, yyyy")
                        ?? DateTime.Now.ToString("MMMM d, yyyy");
                    AddCenteredParagraph(body, dateStr, fontName, fontSize, false, lineSpacing);
                }
                else
                {
                    // Professional: Author Note
                    AddEmptyParagraph(body, fontName, fontSize, lineSpacing);
                    AddEmptyParagraph(body, fontName, fontSize, lineSpacing);
                    AddCenteredParagraph(body, "Author Note", fontName, fontSize, true, lineSpacing);

                    var noteText = $"{paper.AuthorName ?? "Author"}, {paper.Institution ?? "Institution"}.";
                    AddBodyParagraphs(body, noteText, fontName, fontSize, lineSpacing);

                    var corrText = $"Correspondence concerning this paper should be addressed to {paper.AuthorName ?? "the author"}.";
                    AddBodyParagraphs(body, corrText, fontName, fontSize, lineSpacing);
                }

                body.AppendChild(new Paragraph(
                    new Run(new Break { Type = BreakValues.Page })));
            }

            // ═══ BODY ═══
            AddCenteredParagraph(body, paper.Title, fontName, fontSize, true, lineSpacing);

            var isFirstSection = true;
            foreach (var section in sections)
            {
                var text = GetSectionText(section);

                if (isFirstSection)
                {
                    AddBodyParagraphs(body, text, fontName, fontSize, lineSpacing);
                    isFirstSection = false;
                }
                else
                {
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

    /// <summary>
    /// APA 7 Professional: Running head is a shortened title (max 50 chars), ALL CAPS.
    /// </summary>
    private static string GetRunningHead(string title)
    {
        var shortened = title.Length > 50 ? title[..50].Trim() : title;
        return shortened.ToUpperInvariant();
    }

    /// <summary>
    /// APA 7 Professional DOCX header: running head left-aligned + page number right-aligned.
    /// </summary>
    private static void AddRunningHeadWithPageNumbers(
        MainDocumentPart mainPart, string runningHead, string fontName, string fontSize)
    {
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        var headerId = mainPart.GetIdOfPart(headerPart);

        // Create a tab stop for right-aligned page number
        headerPart.Header = new Header(
            new Paragraph(
                new ParagraphProperties(
                    new Tabs(new TabStop { Val = TabStopValues.Right, Position = 9360 })),
                // Running head text
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = fontName, HighAnsi = fontName },
                        new FontSize { Val = fontSize }),
                    new Text(runningHead) { Space = SpaceProcessingModeValues.Preserve }),
                // Tab to right
                new Run(new TabChar()),
                // Page number
                new Run(
                    new RunProperties(new FontSize { Val = fontSize }),
                    new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(
                    new RunProperties(new FontSize { Val = fontSize }),
                    new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new RunProperties(new FontSize { Val = fontSize }),
                    new FieldChar { FieldCharType = FieldCharValues.End })));

        headerPart.Header.Save();

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
