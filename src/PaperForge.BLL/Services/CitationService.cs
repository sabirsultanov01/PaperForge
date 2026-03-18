using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.BLL.Services;

public class CitationService : ICitationService
{
    // ═══════════════════════════════════
    // IN-TEXT CITATIONS
    // ═══════════════════════════════════

    public string FormatInTextCitation(Reference r, CitationStyle style) => style switch
    {
        CitationStyle.APA7 => FormatApa7InText(r),
        CitationStyle.MLA9 => $"({r.AuthorLastName}{(r.Pages is not null ? " " + r.Pages : "")})",
        CitationStyle.Chicago17 => $"({r.AuthorLastName} {YearOrNd(r.Year)}{(r.Pages is not null ? ", " + r.Pages : "")})",
        _ => FormatApa7InText(r)
    };

    /// <summary>
    /// APA 7 in-text: (Author, Year) or ("Title...", Year) when no author.
    /// </summary>
    private static string FormatApa7InText(Reference r)
    {
        var author = !string.IsNullOrWhiteSpace(r.AuthorLastName)
            ? r.AuthorLastName
            : TruncateTitle(r.Title);
        return $"({author}, {YearOrNd(r.Year)})";
    }

    // ═══════════════════════════════════
    // BIBLIOGRAPHY
    // ═══════════════════════════════════

    public string FormatBibliographyEntry(Reference r, CitationStyle style) => style switch
    {
        CitationStyle.APA7 => FormatApa7(r),
        CitationStyle.MLA9 => FormatMla9(r),
        CitationStyle.Chicago17 => FormatChicago17(r),
        _ => FormatApa7(r)
    };

    public List<string> FormatBibliography(IEnumerable<Reference> references, CitationStyle style)
        => references
            .OrderBy(r => r.AuthorLastName)
            .ThenBy(r => r.Year)
            .Select(r => FormatBibliographyEntry(r, style))
            .ToList();

    // ═══════════════════════════════════════════════════════════════
    // APA 7TH EDITION
    // Per: https://pitt.libguides.com/citationhelp/apa7
    //
    // Book:
    //   Author, A. A. (Year). Title of work: Capital letter also for subtitle. Publisher.
    //
    // Journal Article:
    //   Author, A. A. (Year). Title of article. Title of Periodical, Volume(Issue), pp–pp.
    //       https://doi.org/xxxxx
    //
    // Website:
    //   Author, A. A. (Year, Month Day). Title of page. Site Name. https://url
    //
    // Conference Paper:
    //   Author, A. A. (Year). Title of paper. In Editor (Ed.), Title of proceedings
    //       (pp. xx–xx). Publisher.
    // ═══════════════════════════════════════════════════════════════

    private static string FormatApa7(Reference r) => r.ReferenceType switch
    {
        ReferenceType.Book => FormatApa7Book(r),
        ReferenceType.JournalArticle => FormatApa7Journal(r),
        ReferenceType.Website => FormatApa7Website(r),
        ReferenceType.ConferencePaper => FormatApa7Conference(r),
        _ => $"{AuthorApa(r)} ({YearOrNd(r.Year)}). {r.Title}."
    };

    // Book: Author, A. A. (Year). Title of work. Publisher. https://doi.org/xxx
    private static string FormatApa7Book(Reference r)
    {
        var entry = $"{AuthorApa(r)} ({YearOrNd(r.Year)}). {r.Title}.";

        if (!string.IsNullOrWhiteSpace(r.Publisher))
            entry += $" {r.Publisher}.";

        entry += AppendDoiOrUrl(r);

        return entry;
    }

    // Journal: Author, A. A. (Year). Title of article. Journal Title, Volume(Issue), pp–pp.
    //          https://doi.org/xxx
    private static string FormatApa7Journal(Reference r)
    {
        var entry = $"{AuthorApa(r)} ({YearOrNd(r.Year)}). {r.Title}.";

        if (!string.IsNullOrWhiteSpace(r.Journal))
        {
            entry += $" {r.Journal}";

            if (!string.IsNullOrWhiteSpace(r.Volume))
            {
                entry += $", {r.Volume}";
                if (!string.IsNullOrWhiteSpace(r.Issue))
                    entry += $"({r.Issue})";
            }

            if (!string.IsNullOrWhiteSpace(r.Pages))
                entry += $", {r.Pages}";

            entry += ".";
        }

        entry += AppendDoiOrUrl(r);

        return entry;
    }

    // Website: Author, A. A. (Year, Month Day). Title of page. Site Name. https://url
    private static string FormatApa7Website(Reference r)
    {
        var dateStr = FormatApa7Date(r.Year, r.AccessDate);
        var entry = $"{AuthorApa(r)} ({dateStr}). {r.Title}.";

        if (!string.IsNullOrWhiteSpace(r.Publisher))
            entry += $" {r.Publisher}.";

        if (!string.IsNullOrWhiteSpace(r.URL))
            entry += $" {r.URL}";

        return entry;
    }

    // Conference: Author, A. A. (Year). Title of paper. In Title of proceedings (pp. xx–xx).
    //             Publisher. https://doi.org/xxx
    private static string FormatApa7Conference(Reference r)
    {
        var entry = $"{AuthorApa(r)} ({YearOrNd(r.Year)}). {r.Title}.";

        var proceedingsTitle = !string.IsNullOrWhiteSpace(r.Journal)
            ? r.Journal
            : "Conference Proceedings";

        entry += $" In {proceedingsTitle}";

        if (!string.IsNullOrWhiteSpace(r.Pages))
            entry += $" (pp. {r.Pages})";

        entry += ".";

        if (!string.IsNullOrWhiteSpace(r.Publisher))
            entry += $" {r.Publisher}.";

        entry += AppendDoiOrUrl(r);

        return entry;
    }

    // ═══════════════════════════════════
    // APA 7 HELPERS
    // ═══════════════════════════════════

    /// <summary>
    /// Formats author as "LastName, F." with proper spacing.
    /// If no author name, returns empty string so the title takes precedence.
    /// </summary>
    private static string AuthorApa(Reference r)
    {
        if (string.IsNullOrWhiteSpace(r.AuthorLastName))
            return "";

        if (string.IsNullOrWhiteSpace(r.AuthorFirstName))
            return r.AuthorLastName;

        return $"{r.AuthorLastName}, {r.AuthorFirstName[0]}.";
    }

    /// <summary>
    /// Returns "n.d." if year is null, otherwise the year string.
    /// </summary>
    private static string YearOrNd(int? year) => year?.ToString() ?? "n.d.";

    /// <summary>
    /// Formats DOI as full URL: https://doi.org/10.xxxx
    /// Per APA 7, DOIs should always be presented as URLs.
    /// </summary>
    private static string FormatDoi(string doi)
    {
        doi = doi.Trim();
        if (doi.StartsWith("https://doi.org/", StringComparison.OrdinalIgnoreCase))
            return doi;
        if (doi.StartsWith("http://doi.org/", StringComparison.OrdinalIgnoreCase))
            return doi.Replace("http://", "https://");
        if (doi.StartsWith("doi:", StringComparison.OrdinalIgnoreCase))
            doi = doi[4..].Trim();
        return $"https://doi.org/{doi}";
    }

    /// <summary>
    /// Appends DOI (preferred) or URL to entry. Returns " https://doi.org/..." or " https://..."
    /// </summary>
    private static string AppendDoiOrUrl(Reference r)
    {
        if (!string.IsNullOrWhiteSpace(r.DOI))
            return $" {FormatDoi(r.DOI)}";
        if (!string.IsNullOrWhiteSpace(r.URL))
            return $" {r.URL}";
        return "";
    }

    /// <summary>
    /// Formats date for APA 7 websites: (Year, Month Day) or (Year) or (n.d.)
    /// </summary>
    private static string FormatApa7Date(int? year, DateTime? accessDate)
    {
        if (year is null && accessDate is null)
            return "n.d.";

        if (year is not null && accessDate is not null)
            return $"{year}, {accessDate.Value:MMMM d}";

        return year?.ToString() ?? "n.d.";
    }

    /// <summary>
    /// Truncates title to first few words for in-text citation when no author.
    /// APA 7: use first few words of the title in quotes.
    /// </summary>
    private static string TruncateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Untitled";
        var words = title.Split(' ');
        var shortened = string.Join(" ", words.Take(4));
        if (words.Length > 4) shortened += "...";
        return $"\"{shortened}\"";
    }

    // ═══════════════════════════════════
    // MLA 9TH
    // ═══════════════════════════════════

    private static string FormatMla9(Reference r) => r.ReferenceType switch
    {
        ReferenceType.Book =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. {r.Title}. {r.Publisher}, {r.Year}.",

        ReferenceType.JournalArticle =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. \"{r.Title}.\" {r.Journal ?? ""}, " +
            $"vol. {r.Volume}, no. {r.Issue}, {r.Year}, pp. {r.Pages}." +
            $"{(!string.IsNullOrWhiteSpace(r.DOI) ? $" {FormatDoi(r.DOI)}" : "")}",

        ReferenceType.Website =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. \"{r.Title}.\" {r.Publisher ?? ""}, " +
            $"{r.Year}. {r.URL}.",

        ReferenceType.ConferencePaper =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. \"{r.Title}.\" " +
            $"{r.Journal ?? "Conference Proceedings"}, {r.Year}, pp. {r.Pages}.",

        _ => $"{r.AuthorLastName}, {r.AuthorFirstName}. {r.Title}. {r.Year}."
    };

    // ═══════════════════════════════════
    // CHICAGO 17TH
    // ═══════════════════════════════════

    private static string FormatChicago17(Reference r) => r.ReferenceType switch
    {
        ReferenceType.Book =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. {r.Title}. {r.Publisher}, {r.Year}.",

        ReferenceType.JournalArticle =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. \"{r.Title}.\" {r.Journal ?? ""} " +
            $"{r.Volume}, no. {r.Issue} ({r.Year}): {r.Pages}." +
            $"{(!string.IsNullOrWhiteSpace(r.DOI) ? $" {FormatDoi(r.DOI)}" : "")}",

        ReferenceType.Website =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. \"{r.Title}.\" {r.Publisher}. " +
            $"{(r.AccessDate.HasValue ? $"Accessed {r.AccessDate:MMMM d, yyyy}. " : "")}{r.URL}.",

        ReferenceType.ConferencePaper =>
            $"{r.AuthorLastName}, {r.AuthorFirstName}. \"{r.Title}.\" " +
            $"Paper presented at {r.Journal ?? "conference"}, {r.Year}.",

        _ => $"{r.AuthorLastName}, {r.AuthorFirstName}. {r.Title}. {r.Year}."
    };
}
