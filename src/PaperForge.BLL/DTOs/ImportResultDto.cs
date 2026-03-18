namespace PaperForge.BLL.DTOs;

public class ImportResultDto
{
    public bool Success { get; set; }
    public Guid? PaperId { get; set; }
    public string? ErrorMessage { get; set; }
    public int WordCount { get; set; }
    public int SectionsCreated { get; set; }
}
