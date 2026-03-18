using PaperForge.DAL.Enums;

namespace PaperForge.DAL.Entities;

public class Paper
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? AuthorName { get; set; }
    public string? Institution { get; set; }
    public string? CourseName { get; set; }
    public string? InstructorName { get; set; }
    public PaperType PaperType { get; set; }
    public CitationStyle CitationStyle { get; set; } = CitationStyle.APA7;
    public DateTime? Deadline { get; set; }
    public PaperStatus Status { get; set; } = PaperStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Reference> References { get; set; } = new List<Reference>();
}
