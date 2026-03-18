using PaperForge.DAL.Enums;

namespace PaperForge.BLL.DTOs;

public class AddReferenceDto
{
    public ReferenceType ReferenceType { get; set; }
    public string? AuthorLastName { get; set; }
    public string? AuthorFirstName { get; set; }
    public int? Year { get; set; }
    public string? Title { get; set; }
    public string? Publisher { get; set; }
    public string? Journal { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public string? DOI { get; set; }
    public string? URL { get; set; }
}
