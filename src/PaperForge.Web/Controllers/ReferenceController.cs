using Microsoft.AspNetCore.Mvc;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReferenceController : ControllerBase
{
    private readonly IReferenceService _refService;
    private readonly ICitationService _citationService;
    private readonly ICrossRefService _crossRefService;

    public ReferenceController(
        IReferenceService refService,
        ICitationService citationService,
        ICrossRefService crossRefService)
    {
        _refService = refService;
        _citationService = citationService;
        _crossRefService = crossRefService;
    }

    [HttpGet("{paperId}")]
    public async Task<IActionResult> GetAll(Guid paperId)
    {
        var refs = await _refService.GetReferencesAsync(paperId);
        return Ok(refs);
    }

    [HttpPost("{paperId}")]
    public async Task<IActionResult> Add(Guid paperId, [FromBody] Reference reference)
    {
        if (string.IsNullOrWhiteSpace(reference.AuthorLastName) && string.IsNullOrWhiteSpace(reference.Title))
            return BadRequest(new { error = "Author last name or title is required." });

        var added = await _refService.AddReferenceAsync(paperId, reference);
        return Ok(added);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _refService.DeleteReferenceAsync(id);
        return Ok();
    }

    [HttpGet("doi/{*doi}")]
    public async Task<IActionResult> LookupDoi(string doi)
    {
        if (string.IsNullOrWhiteSpace(doi))
            return BadRequest(new { error = "DOI is required." });

        var result = await _crossRefService.LookupDoiAsync(doi);
        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage ?? "DOI not found." });

        return Ok(result);
    }

    [HttpGet("cite/{paperId}")]
    public async Task<IActionResult> GetCitations(Guid paperId, [FromQuery] CitationStyle style)
    {
        var refs = await _refService.GetReferencesAsync(paperId);
        var citations = refs.Select(r => new
        {
            r.Id,
            InText = _citationService.FormatInTextCitation(r, style),
            Bibliography = _citationService.FormatBibliographyEntry(r, style),
        });
        return Ok(citations);
    }
}
