using PaperForge.DAL.Enums;

namespace PaperForge.Web.ViewModels;

public class CreatePaperViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? AuthorName { get; set; }
    public string? Institution { get; set; }
    public string? CourseName { get; set; }
    public string? InstructorName { get; set; }
    public PaperType PaperType { get; set; }
    public CitationStyle CitationStyle { get; set; } = CitationStyle.APA7;
    public DateTime? Deadline { get; set; }
}
