using System.Collections.Concurrent;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.DAL.Repositories;

public class SectionRepository : ISectionRepository
{
    private static readonly ConcurrentDictionary<Guid, Section> _sections = new();

    public Task<List<Section>> GetByPaperIdAsync(Guid paperId)
        => Task.FromResult(_sections.Values
            .Where(s => s.PaperId == paperId)
            .OrderBy(s => s.OrderIndex)
            .ToList());

    public Task<Section?> GetByIdAsync(Guid id)
        => Task.FromResult(_sections.GetValueOrDefault(id));

    public Task AddAsync(Section section)
    {
        _sections[section.Id] = section;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Section section)
    {
        section.UpdatedAt = DateTime.UtcNow;
        _sections[section.Id] = section;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _sections.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task ReorderAsync(Guid paperId, List<Guid> orderedIds)
    {
        var sections = _sections.Values
            .Where(s => s.PaperId == paperId)
            .ToList();

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var section = sections.FirstOrDefault(s => s.Id == orderedIds[i]);
            if (section is not null)
                section.OrderIndex = i;
        }

        return Task.CompletedTask;
    }
}
