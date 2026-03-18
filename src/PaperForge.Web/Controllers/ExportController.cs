using Microsoft.AspNetCore.Mvc;
using PaperForge.BLL.DTOs;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Enums;

namespace PaperForge.Web.Controllers;

[Route("api/[controller]")]
public class ExportController : Controller
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
        => _exportService = exportService;

    [HttpGet("download/{paperId}")]
    public async Task<IActionResult> Download(
        Guid paperId,
        [FromQuery] ExportFormat format = ExportFormat.PDF,
        [FromQuery] string font = "Times New Roman",
        [FromQuery] int fontSize = 12,
        [FromQuery] double lineSpacing = 2.0,
        [FromQuery] bool titlePage = true)
    {
        var opts = new ExportOptionsDto
        {
            Format = format,
            FontFamily = font,
            FontSize = fontSize,
            LineSpacing = lineSpacing,
            IncludeTitlePage = titlePage,
        };

        try
        {
            var (bytes, fileName, contentType) =
                await _exportService.ExportPaperAsync(paperId, opts);
            return File(bytes, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
