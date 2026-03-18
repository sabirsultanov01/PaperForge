using Microsoft.AspNetCore.Mvc;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Enums;

namespace PaperForge.Web.Controllers;

public class ImportController : Controller
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
        => _importService = importService;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string title,
        [FromForm] PaperType paperType = PaperType.AcademicEssay,
        [FromForm] CitationStyle citationStyle = CitationStyle.APA7)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("File too large. Maximum 10 MB.");

        using var stream = file.OpenReadStream();
        var result = await _importService.ImportFileAsync(
            stream,
            file.FileName,
            string.IsNullOrWhiteSpace(title)
                ? Path.GetFileNameWithoutExtension(file.FileName)
                : title,
            paperType,
            citationStyle);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return RedirectToAction("Workspace", "Paper", new { id = result.PaperId });
    }
}
