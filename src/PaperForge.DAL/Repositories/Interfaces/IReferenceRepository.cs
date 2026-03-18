using PaperForge.DAL.Entities;

namespace PaperForge.DAL.Repositories.Interfaces;

public interface IReferenceRepository
{
    Task<List<Reference>> GetByPaperIdAsync(Guid paperId);
    Task<Reference?> GetByIdAsync(Guid id);
    Task AddAsync(Reference reference);
    Task UpdateAsync(Reference reference);
    Task DeleteAsync(Guid id);
}
