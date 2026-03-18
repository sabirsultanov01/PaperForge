using PaperForge.DAL.Entities;

namespace PaperForge.DAL.Repositories.Interfaces;

public interface ISectionRepository
{
    Task<List<Section>> GetByPaperIdAsync(Guid paperId);
    Task<Section?> GetByIdAsync(Guid id);
    Task AddAsync(Section section);
    Task UpdateAsync(Section section);
    Task DeleteAsync(Guid id);
    Task ReorderAsync(Guid paperId, List<Guid> orderedIds);
}
