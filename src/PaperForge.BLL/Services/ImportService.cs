using PaperForge.BLL.DTOs;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

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
            var extractedSections = ext switch
            {
                ".docx" => ExtractFromDocx(fileStream),
                ".pdf"  => ExtractFromPdf(fileStream),
                ".txt"  => ExtractFromTxt(fileStream),
                _ => [("Imported Content", "")]
            };

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
            for (var i = 0; i < extractedSections.Count; i++)
            {
                var (title, content) = extractedSections[i];
                var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                totalWords += wordCount;

                var section = new Section
                {
                    Id = Guid.NewGuid(),
                    PaperId = paper.Id,
                    Title = title,
                    OrderIndex = i,
                    WordTarget = Math.Max(wordCount, 200),
                    PlainText = content,
                    Content = ToQuillDelta(content),
                    Status = wordCount > 0 ? SectionStatus.InProgress : SectionStatus.NotStarted,
                };
                await _sectionRepo.AddAsync(section);
            }

            return new ImportResultDto
            {
                Success = true,
                PaperId = paper.Id,
                WordCount = totalWords,
                SectionsCreated = extractedSections.Count,
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

    private static List<(string Title, string Content)> ExtractFromPdf(Stream stream)
    {
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Position = 0;

        using var document = PdfDocument.Open(memStream);

        // Collect all lines from all pages
        var allLines = new List<string>();
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            // Group words into lines by Y-position (same baseline = same line)
            var lineGroups = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                .OrderByDescending(g => g.Key);

            foreach (var lineGroup in lineGroups)
            {
                var lineText = string.Join(" ", lineGroup.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                allLines.Add(lineText);
            }
        }

        if (allLines.Count == 0)
            return [("Imported Content", "")];

        // Detect sections by looking for short lines that look like headings
        // Heuristics: line is short (< 80 chars), not ending with period, surrounded by blank-ish context
        var sections = new List<(string Title, string Content)>();
        var currentTitle = "Imported Content";
        var currentContent = new System.Text.StringBuilder();

        // Common academic section heading patterns
        var headingPatterns = new[]
        {
            "abstract", "introduction", "background", "literature review",
            "methodology", "methods", "materials and methods", "research design",
            "results", "findings", "discussion", "analysis",
            "conclusion", "conclusions", "summary",
            "recommendations", "limitations", "future work",
            "references", "bibliography", "works cited", "appendix",
            "acknowledgements", "acknowledgments"
        };

        for (var i = 0; i < allLines.Count; i++)
        {
            var line = allLines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var isHeading = false;

            // Check if the line matches a known heading pattern
            var lineLower = line.ToLowerInvariant().TrimEnd('.', ':', ' ');
            if (headingPatterns.Any(p => lineLower == p || lineLower.StartsWith(p + ":") ||
                System.Text.RegularExpressions.Regex.IsMatch(lineLower, @"^\d+\.?\s*" + p)))
            {
                isHeading = true;
            }
            // Also detect numbered headings like "1. Introduction" or "II. Methods"
            else if (line.Length < 80 && !line.EndsWith('.') &&
                     System.Text.RegularExpressions.Regex.IsMatch(line, @"^(\d+\.?\s+|[IVX]+\.?\s+|[A-Z]\.\s+)[A-Z]"))
            {
                isHeading = true;
            }
            // Short ALL CAPS lines are likely headings
            else if (line.Length < 60 && line.Length > 2 && line == line.ToUpperInvariant() &&
                     line.Any(char.IsLetter) && !line.EndsWith('.'))
            {
                isHeading = true;
            }

            if (isHeading)
            {
                if (currentContent.Length > 0 || sections.Count > 0)
                {
                    sections.Add((currentTitle, currentContent.ToString().Trim()));
                    currentContent.Clear();
                }
                // Clean up heading: remove numbering prefix for display
                currentTitle = System.Text.RegularExpressions.Regex.Replace(
                    line, @"^(\d+\.?\s+|[IVX]+\.?\s+)", "").Trim();
                if (string.IsNullOrWhiteSpace(currentTitle))
                    currentTitle = line;
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        // Add the last section
        if (currentContent.Length > 0)
            sections.Add((currentTitle, currentContent.ToString().Trim()));

        // If no sections were detected, return everything as one section
        if (sections.Count == 0)
            return [("Imported Content", string.Join("\n", allLines).Trim())];

        // Filter out empty sections
        sections = sections.Where(s => !string.IsNullOrWhiteSpace(s.Content)).ToList();

        return sections.Count > 0 ? sections : [("Imported Content", string.Join("\n", allLines).Trim())];
    }

    private static List<(string Title, string Content)> ExtractFromTxt(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd().Trim();
        return [("Imported Content", text)];
    }

    private static string ToQuillDelta(string plainText)
    {
        var escaped = plainText
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "")
            .Replace("\t", "\\t");

        return $$"""{"ops":[{"insert":"{{escaped}}\n"}]}""";
    }
}
