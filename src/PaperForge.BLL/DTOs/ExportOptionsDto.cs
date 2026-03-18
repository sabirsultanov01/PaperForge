using PaperForge.DAL.Enums;

namespace PaperForge.BLL.DTOs;

public class ExportOptionsDto
{
    public ExportFormat Format { get; set; } = ExportFormat.PDF;
    public string FontFamily { get; set; } = "Times New Roman";
    public int FontSize { get; set; } = 12;
    public double LineSpacing { get; set; } = 2.0;
    public bool IncludeTitlePage { get; set; } = true;
    /// <summary>
    /// True = APA 7 Student format (no running head, no author note).
    /// False = APA 7 Professional format (running head, author note).
    /// </summary>
    public bool IsStudentPaper { get; set; } = true;
}
