using System.Collections.Concurrent;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.DAL.Repositories;

public class PaperRepository : IPaperRepository
{
    private static readonly ConcurrentDictionary<Guid, Paper> _papers = new();

    private readonly ISectionRepository _sectionRepo;
    private readonly IReferenceRepository _referenceRepo;

    public PaperRepository(ISectionRepository sectionRepo, IReferenceRepository referenceRepo)
    {
        _sectionRepo = sectionRepo;
        _referenceRepo = referenceRepo;
    }

    public async Task<List<Paper>> GetAllAsync()
    {
        var papers = _papers.Values.OrderByDescending(p => p.UpdatedAt).ToList();
        foreach (var paper in papers)
            paper.Sections = await _sectionRepo.GetByPaperIdAsync(paper.Id);
        return papers;
    }

    public Task<Paper?> GetByIdAsync(Guid id)
        => Task.FromResult(_papers.GetValueOrDefault(id));

    public async Task<Paper?> GetWithSectionsAsync(Guid id)
    {
        var paper = _papers.GetValueOrDefault(id);
        if (paper is not null)
            paper.Sections = await _sectionRepo.GetByPaperIdAsync(paper.Id);
        return paper;
    }

    public async Task<Paper?> GetWithAllAsync(Guid id)
    {
        var paper = _papers.GetValueOrDefault(id);
        if (paper is not null)
        {
            paper.Sections = await _sectionRepo.GetByPaperIdAsync(paper.Id);
            paper.References = await _referenceRepo.GetByPaperIdAsync(paper.Id);
        }
        return paper;
    }

    public Task AddAsync(Paper paper)
    {
        _papers[paper.Id] = paper;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Paper paper)
    {
        paper.UpdatedAt = DateTime.UtcNow;
        _papers[paper.Id] = paper;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _papers.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
