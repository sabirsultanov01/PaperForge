using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.BLL.Services;

public class PaperService : IPaperService
{
    private readonly IPaperRepository _paperRepo;
    private readonly ISectionRepository _sectionRepo;
    private readonly IOutlineGeneratorService _outlineService;

    public PaperService(
        IPaperRepository paperRepo,
        ISectionRepository sectionRepo,
        IOutlineGeneratorService outlineService)
    {
        _paperRepo = paperRepo;
        _sectionRepo = sectionRepo;
        _outlineService = outlineService;
    }

    public Task<List<Paper>> GetAllPapersAsync() => _paperRepo.GetAllAsync();
    public Task<Paper?> GetPaperAsync(Guid id) => _paperRepo.GetByIdAsync(id);
    public Task<Paper?> GetPaperWithSectionsAsync(Guid id) => _paperRepo.GetWithSectionsAsync(id);
    public Task<Paper?> GetPaperWithAllAsync(Guid id) => _paperRepo.GetWithAllAsync(id);

    public async Task<Paper> CreatePaperAsync(
        string title, PaperType paperType, CitationStyle citationStyle,
        string? subject, string? authorName, string? institution,
        string? courseName, string? instructorName, DateTime? deadline)
    {
        var paper = new Paper
        {
            Id = Guid.NewGuid(),
            Title = title,
            PaperType = paperType,
            CitationStyle = citationStyle,
            Subject = subject,
            AuthorName = authorName,
            Institution = institution,
            CourseName = courseName,
            InstructorName = instructorName,
            Deadline = deadline,
        };
        await _paperRepo.AddAsync(paper);

        var outline = await _outlineService.GenerateOutlineAsync(paperType);
        for (var i = 0; i < outline.Count; i++)
        {
            var s = outline[i];
            await _sectionRepo.AddAsync(new Section
            {
                Id = Guid.NewGuid(),
                PaperId = paper.Id,
                Title = s.Title,
                OrderIndex = i,
                WordTarget = s.WordTarget,
                GuidanceText = s.Guidance,
            });
        }

        return paper;
    }

    public Task UpdatePaperAsync(Paper paper) => _paperRepo.UpdateAsync(paper);
    public Task DeletePaperAsync(Guid id) => _paperRepo.DeleteAsync(id);
}
