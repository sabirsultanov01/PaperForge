using PaperForge.BLL.DTOs;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PaperForge.BLL.Services;

public class ImportService : IImportService
{
    private readonly IPaperRepository _paperRepo;
    private readonly ISectionRepository _sectionRepo;

    private static readonly HashSet<string> SupportedExtensions = [".docx", ".txt", ".pdf"];

    public ImportService(IPaperRepository paperRepo, ISectionRepository sectionRepo)
    {
        _paperRepo = paperRepo;
        _sectionRepo = sectionRepo;
    }

    public async Task<ImportResultDto> ImportFileAsync(
        Stream fileStream, string fileName,
        string paperTitle, PaperType paperType,
        CitationStyle citationStyle)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(ext))
            return new ImportResultDto
            {
                Success = false,
                ErrorMessage = $"Unsupported file type: {ext}. Supported: .docx, .txt, .pdf"
            };

        try
        {
            // PDF returns rich sections with line metadata; others return plain text
            List<RichSection> richSections;
            if (ext == ".pdf")
            {
                richSections = ExtractRichFromPdf(fileStream);
            }
            else
            {
                var extractedSections = ext switch
                {
                    ".docx" => ExtractFromDocx(fileStream),
                    ".txt"  => ExtractFromTxt(fileStream),
                    _ => [("Imported Content", "")]
                };
                richSections = extractedSections
                    .Select(s => new RichSection(s.Title, s.Content, []))
                    .ToList();
            }

            var paper = new Paper
            {
                Id = Guid.NewGuid(),
                Title = paperTitle,
                PaperType = paperType,
                CitationStyle = citationStyle,
                Status = PaperStatus.InProgress,
            };
            await _paperRepo.AddAsync(paper);

            var totalWords = 0;
            for (var i = 0; i < richSections.Count; i++)
            {
                var rs = richSections[i];
                var wordCount = rs.PlainText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                totalWords += wordCount;

                var section = new Section
                {
                    Id = Guid.NewGuid(),
                    PaperId = paper.Id,
                    Title = rs.Title,
                    OrderIndex = i,
                    WordTarget = 0,
                    PlainText = rs.PlainText,
                    Content = rs.Lines.Count > 0
                        ? BuildRichQuillDelta(rs.Lines)
                        : ToQuillDelta(rs.PlainText),
                    Status = wordCount > 0 ? SectionStatus.InProgress : SectionStatus.NotStarted,
                };
                await _sectionRepo.AddAsync(section);
            }

            return new ImportResultDto
            {
                Success = true,
                PaperId = paper.Id,
                WordCount = totalWords,
                SectionsCreated = richSections.Count,
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}"
            };
        }
    }

    private static List<(string Title, string Content)> ExtractFromDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null)
            return [("Imported Content", "")];

        var sections = new List<(string Title, string Content)>();
        var currentTitle = "Imported Content";
        var currentContent = new System.Text.StringBuilder();

        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
            var isHeading = styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase);

            if (isHeading)
            {
                if (currentContent.Length > 0)
                {
                    sections.Add((currentTitle, currentContent.ToString().Trim()));
                    currentContent.Clear();
                }
                currentTitle = para.InnerText.Trim();
                if (string.IsNullOrEmpty(currentTitle))
                    currentTitle = "Untitled Section";
            }
            else
            {
                var text = para.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                    currentContent.AppendLine(text);
            }
        }

        if (currentContent.Length > 0)
            sections.Add((currentTitle, currentContent.ToString().Trim()));

        return sections.Count > 0 ? sections : [("Imported Content", "")];
    }

    // ═══════════════════════════════════
    // PDF IMPORT — Extracts text with formatting metadata (bold, font size, italic)
    // ═══════════════════════════════════

    private static List<PdfLine> ExtractPdfLines(Stream stream)
    {
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Position = 0;

        using var document = PdfDocument.Open(memStream);
        var pdfLines = new List<PdfLine>();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            var letters = page.Letters.ToList();
            var lettersByY = letters
                .GroupBy(l => Math.Round(l.GlyphRectangle.Bottom, 0))
                .ToDictionary(g => g.Key, g => g.ToList());

            var lineGroups = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                .OrderByDescending(g => g.Key);

            foreach (var lineGroup in lineGroups)
            {
                var sortedWords = lineGroup.OrderBy(w => w.BoundingBox.Left).ToList();
                var lineText = string.Join(" ", sortedWords.Select(w => w.Text));
                if (string.IsNullOrWhiteSpace(lineText)) continue;

                var lineY = Math.Round(lineGroup.Key, 0);
                var nearbyLetters = new List<Letter>();
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (lettersByY.TryGetValue(lineY + dy, out var found))
                        nearbyLetters.AddRange(found);
                }

                double avgFontSize = 12;
                var isBold = false;
                var isItalic = false;
                if (nearbyLetters.Count > 0)
                {
                    avgFontSize = nearbyLetters.Average(l => l.PointSize);
                    isBold = nearbyLetters.Count(l =>
                        l.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true)
                        > nearbyLetters.Count / 2;
                    isItalic = nearbyLetters.Count(l =>
                        l.FontName?.Contains("Italic", StringComparison.OrdinalIgnoreCase) == true ||
                        l.FontName?.Contains("Oblique", StringComparison.OrdinalIgnoreCase) == true)
                        > nearbyLetters.Count / 2;
                }

                pdfLines.Add(new PdfLine
                {
                    Text = lineText.Trim(),
                    FontSize = avgFontSize,
                    IsBold = isBold,
                    IsItalic = isItalic,
                    AvgX = sortedWords.Average(w => w.BoundingBox.Left),
                });
            }
        }

        return pdfLines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToList();
    }

    private static List<RichSection> ExtractRichFromPdf(Stream stream)
    {
        var pdfLines = ExtractPdfLines(stream);

        if (pdfLines.Count == 0)
            return [new RichSection("Imported Content", "", [])];

        // Determine the "body" font size (most common among longer lines)
        var bodyFontSize = pdfLines
            .Where(l => l.Text.Length > 30)
            .GroupBy(l => Math.Round(l.FontSize, 0))
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? 12;

        // Split into sections, keeping PdfLine references for each section
        var sections = new List<RichSection>();
        var currentTitle = "Imported Content";
        var currentLines = new List<PdfLine>();
        var currentPlain = new System.Text.StringBuilder();

        foreach (var line in pdfLines)
        {
            var isHeading = IsHeadingLine(line, bodyFontSize);

            if (isHeading)
            {
                if (currentPlain.Length > 0 || sections.Count > 0)
                {
                    sections.Add(new RichSection(
                        currentTitle,
                        currentPlain.ToString().Trim(),
                        new List<PdfLine>(currentLines)));
                    currentLines.Clear();
                    currentPlain.Clear();
                }

                currentTitle = CleanHeadingText(line.Text);
                if (string.IsNullOrWhiteSpace(currentTitle))
                    currentTitle = line.Text;
            }
            else
            {
                // Store the line with its font metadata
                line.BodyFontSize = bodyFontSize;
                currentLines.Add(line);
                currentPlain.AppendLine(line.Text);
            }
        }

        if (currentPlain.Length > 0)
            sections.Add(new RichSection(
                currentTitle,
                currentPlain.ToString().Trim(),
                new List<PdfLine>(currentLines)));

        sections = sections.Where(s => !string.IsNullOrWhiteSpace(s.PlainText)).ToList();

        if (sections.Count == 0)
            return [new RichSection("Imported Content",
                string.Join("\n", pdfLines.Select(l => l.Text)).Trim(),
                pdfLines)];

        return sections;
    }

    /// <summary>
    /// Builds a Quill Delta with formatting: bold, italic, and sub-headings from font metadata.
    /// </summary>
    private static string BuildRichQuillDelta(List<PdfLine> lines)
    {
        if (lines.Count == 0) return ToQuillDelta("");

        var ops = new List<object>();
        var bodyFontSize = lines.FirstOrDefault()?.BodyFontSize ?? 12;

        foreach (var line in lines)
        {
            var text = line.Text;
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Determine if this is a sub-heading within the section
            var isSubHeading = line.FontSize > bodyFontSize + 1.0 && text.Length < 100;
            var hasBold = line.IsBold;
            var hasItalic = line.IsItalic;

            // Build the insert op with attributes
            var attributes = new Dictionary<string, object>();
            if (hasBold) attributes["bold"] = true;
            if (hasItalic) attributes["italic"] = true;

            if (attributes.Count > 0)
                ops.Add(new { insert = text, attributes });
            else
                ops.Add(new { insert = text });

            // Add newline — with header attribute if sub-heading
            if (isSubHeading)
                ops.Add(new { insert = "\n", attributes = new { header = 2 } });
            else
                ops.Add(new { insert = "\n" });
        }

        // Ensure delta ends with newline
        if (ops.Count == 0)
            ops.Add(new { insert = "\n" });

        var delta = new { ops };
        return System.Text.Json.JsonSerializer.Serialize(delta);
    }

    private static bool IsHeadingLine(PdfLine line, double bodyFontSize)
    {
        var text = line.Text.Trim();
        if (text.Length < 2 || text.Length > 120) return false;

        // Signal 1: Font size is significantly larger than body text
        var isBiggerFont = line.FontSize > bodyFontSize + 1.5;

        // Signal 2: Line is bold
        var isBold = line.IsBold;

        // Signal 3: Matches known academic heading patterns
        var lineLower = text.ToLowerInvariant().TrimEnd('.', ':', ' ');
        var matchesPattern = HeadingPatterns.Any(p =>
            lineLower == p ||
            lineLower.StartsWith(p + ":") ||
            System.Text.RegularExpressions.Regex.IsMatch(lineLower, @"^\d+\.?\s*" + p));

        // Signal 4: Numbered heading (e.g., "1. Introduction", "II. Methods")
        var isNumbered = System.Text.RegularExpressions.Regex.IsMatch(text,
            @"^(\d+\.?\s+|[IVX]+\.?\s+|[A-Z]\.\s+)[A-Z]");

        // Signal 5: Short ALL-CAPS line
        var isAllCaps = text.Length < 60 && text.Length > 2 &&
            text == text.ToUpperInvariant() && text.Any(char.IsLetter) && !text.EndsWith('.');

        // Signal 6: Short line, doesn't end with period, larger or bold
        var isShortDistinct = text.Length < 80 && !text.EndsWith('.') &&
            (isBiggerFont || isBold);

        // Combine signals — need at least one strong or two weak signals
        if (matchesPattern) return true;
        if (isNumbered) return true;
        if (isAllCaps) return true;
        if (isBiggerFont && isBold) return true;
        if (isBiggerFont && isShortDistinct) return true;
        if (isBold && text.Length < 80 && !text.EndsWith('.')) return true;

        return false;
    }

    private static string CleanHeadingText(string text)
    {
        // Remove numbering prefix like "1. ", "II. ", "A. "
        return System.Text.RegularExpressions.Regex.Replace(
            text, @"^(\d+\.?\s+|[IVX]+\.?\s+)", "").Trim();
    }

    private static readonly string[] HeadingPatterns =
    [
        "abstract", "introduction", "background", "literature review",
        "methodology", "methods", "materials and methods", "research design",
        "theoretical framework", "conceptual framework",
        "results", "findings", "discussion", "analysis", "data analysis",
        "conclusion", "conclusions", "summary", "summary and conclusions",
        "recommendations", "limitations", "future work", "future research",
        "implications", "significance",
        "references", "bibliography", "works cited", "appendix",
        "acknowledgements", "acknowledgments",
        "table of contents", "list of figures", "list of tables",
    ];

    private static List<(string Title, string Content)> ExtractFromTxt(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd().Trim();

        // Try to detect sections in text files by blank-line-separated headings
        var lines = text.Split('\n');
        var sections = new List<(string Title, string Content)>();
        var currentTitle = "Imported Content";
        var currentContent = new System.Text.StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineLower = line.ToLowerInvariant();

            // Check if this line matches a heading pattern
            var isHeading = HeadingPatterns.Any(p => lineLower == p || lineLower.StartsWith(p + ":"));
            if (!isHeading)
                isHeading = System.Text.RegularExpressions.Regex.IsMatch(line, @"^(\d+\.?\s+|[IVX]+\.?\s+)[A-Z]")
                    && line.Length < 80;

            if (isHeading)
            {
                if (currentContent.Length > 0 || sections.Count > 0)
                {
                    sections.Add((currentTitle, currentContent.ToString().Trim()));
                    currentContent.Clear();
                }
                currentTitle = CleanHeadingText(line);
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentContent.Length > 0)
            sections.Add((currentTitle, currentContent.ToString().Trim()));

        sections = sections.Where(s => !string.IsNullOrWhiteSpace(s.Content)).ToList();
        return sections.Count > 0 ? sections : [("Imported Content", text)];
    }

    private static string ToQuillDelta(string plainText)
    {
        // Use System.Text.Json to properly escape the text for JSON
        var textWithTrailingNewline = plainText.TrimEnd('\r', '\n') + "\n";
        var escapedJson = System.Text.Json.JsonSerializer.Serialize(textWithTrailingNewline);
        // escapedJson is a properly quoted JSON string like "\"some text\\n\""
        return $"{{\"ops\":[{{\"insert\":{escapedJson}}}]}}";
    }

    private record RichSection(string Title, string PlainText, List<PdfLine> Lines);

    private class PdfLine
    {
        public string Text { get; set; } = "";
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public double AvgX { get; set; }
        public double BodyFontSize { get; set; } = 12;
    }
}
