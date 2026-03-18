using System.Text.Json.Serialization;
using PaperForge.DAL.Enums;

namespace PaperForge.DAL.Entities;

public class Section
{
    public Guid Id { get; set; }
    public Guid PaperId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int WordTarget { get; set; }
    public string? Content { get; set; }
    public string? PlainText { get; set; }
    public string? GuidanceText { get; set; }
    public SectionStatus Status { get; set; } = SectionStatus.NotStarted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public Paper? Paper { get; set; }
}
