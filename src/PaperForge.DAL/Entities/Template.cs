using PaperForge.DAL.Enums;

namespace PaperForge.DAL.Entities;

public class Template
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PaperType PaperType { get; set; }
    public string StructureJson { get; set; } = string.Empty;
    public string? Description { get; set; }
}
