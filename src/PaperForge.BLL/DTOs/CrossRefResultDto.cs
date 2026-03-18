namespace PaperForge.BLL.DTOs;

public class CrossRefResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string AuthorLastName { get; set; } = string.Empty;
    public string AuthorFirstName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Journal { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public string? Publisher { get; set; }
    public string? DOI { get; set; }
}
