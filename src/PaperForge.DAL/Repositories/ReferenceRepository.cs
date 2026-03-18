using System.Collections.Concurrent;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.DAL.Repositories;

public class ReferenceRepository : IReferenceRepository
{
    private static readonly ConcurrentDictionary<Guid, Reference> _references = new();

    public Task<List<Reference>> GetByPaperIdAsync(Guid paperId)
        => Task.FromResult(_references.Values
            .Where(r => r.PaperId == paperId)
            .OrderBy(r => r.AuthorLastName)
            .ToList());

    public Task<Reference?> GetByIdAsync(Guid id)
        => Task.FromResult(_references.GetValueOrDefault(id));

    public Task AddAsync(Reference reference)
    {
        _references[reference.Id] = reference;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Reference reference)
    {
        _references[reference.Id] = reference;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _references.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
