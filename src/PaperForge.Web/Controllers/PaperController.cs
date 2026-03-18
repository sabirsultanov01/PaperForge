using Microsoft.AspNetCore.Mvc;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.Web.ViewModels;

namespace PaperForge.Web.Controllers;

public class PaperController : Controller
{
    private readonly IPaperService _paperService;
    private readonly IReferenceService _refService;

    public PaperController(IPaperService paperService, IReferenceService refService)
    {
        _paperService = paperService;
        _refService = refService;
    }

    public async Task<IActionResult> Index()
    {
        var papers = await _paperService.GetAllPapersAsync();
        return View(papers);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePaperViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var paper = await _paperService.CreatePaperAsync(
            vm.Title, vm.PaperType, vm.CitationStyle,
            vm.Subject, vm.AuthorName, vm.Institution,
            vm.CourseName, vm.InstructorName, vm.Deadline);

        return RedirectToAction(nameof(Workspace), new { id = paper.Id });
    }

    public async Task<IActionResult> Workspace(Guid id)
    {
        var paper = await _paperService.GetPaperWithSectionsAsync(id);
        if (paper is null) return NotFound();

        var references = await _refService.GetReferencesAsync(id);
        var vm = new WorkspaceViewModel
        {
            Paper = paper,
            Sections = paper.Sections.OrderBy(s => s.OrderIndex).ToList(),
            References = references,
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _paperService.DeletePaperAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
