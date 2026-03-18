using System.Text.Json.Serialization;
using PaperForge.DAL.Enums;

namespace PaperForge.DAL.Entities;

public class Reference
{
    public Guid Id { get; set; }
    public Guid PaperId { get; set; }
    public ReferenceType ReferenceType { get; set; }
    public string AuthorLastName { get; set; } = string.Empty;
    public string AuthorFirstName { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Journal { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public string? DOI { get; set; }
    public string? URL { get; set; }
    public DateTime? AccessDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public Paper? Paper { get; set; }
}
