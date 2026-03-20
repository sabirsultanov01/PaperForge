using Microsoft.AspNetCore.Mvc;
using PaperForge.DAL.Entities;
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

    [HttpPatch("{id}/rename")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameDto dto)
    {
        var section = await _sectionRepo.GetByIdAsync(id);
        if (section is null) return NotFound();

        section.Title = dto.Title?.Trim() ?? section.Title;
        await _sectionRepo.UpdateAsync(section);
        return Ok(new { section.Id, section.Title });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateSectionDto dto)
    {
        var existing = await _sectionRepo.GetByPaperIdAsync(dto.PaperId);
        var section = new Section
        {
            Id = Guid.NewGuid(),
            PaperId = dto.PaperId,
            Title = dto.Title?.Trim() ?? "New Section",
            OrderIndex = existing.Count,
            Status = SectionStatus.NotStarted,
        };
        await _sectionRepo.AddAsync(section);
        return Ok(new { section.Id, section.Title, section.OrderIndex });
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

public class RenameDto
{
    public string? Title { get; set; }
}

public class CreateSectionDto
{
    public Guid PaperId { get; set; }
    public string? Title { get; set; }
}

public class ReorderDto
{
    public Guid PaperId { get; set; }
    public List<Guid> SectionIds { get; set; } = [];
}
