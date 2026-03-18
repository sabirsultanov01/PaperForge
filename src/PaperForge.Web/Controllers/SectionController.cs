using Microsoft.AspNetCore.Mvc;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SectionController : ControllerBase
{
    private readonly ISectionRepository _sectionRepo;

    public SectionController(ISectionRepository sectionRepo) => _sectionRepo = sectionRepo;

    [HttpPut("{id}")]
    public async Task<IActionResult> Save(Guid id, [FromBody] SectionSaveDto dto)
    {
        var section = await _sectionRepo.GetByIdAsync(id);
        if (section is null) return NotFound();

        section.Content = dto.Content;
        section.PlainText = dto.PlainText;
        section.Status = string.IsNullOrWhiteSpace(dto.PlainText)
            ? SectionStatus.NotStarted
            : SectionStatus.InProgress;
        await _sectionRepo.UpdateAsync(section);

        return Ok(new { saved = true, updatedAt = section.UpdatedAt });
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderDto dto)
    {
        await _sectionRepo.ReorderAsync(dto.PaperId, dto.SectionIds);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _sectionRepo.DeleteAsync(id);
        return Ok();
    }
}

public class SectionSaveDto
{
    public string? Content { get; set; }
    public string? PlainText { get; set; }
}

public class ReorderDto
{
    public Guid PaperId { get; set; }
    public List<Guid> SectionIds { get; set; } = [];
}
