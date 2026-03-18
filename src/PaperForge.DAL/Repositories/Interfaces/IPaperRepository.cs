using PaperForge.DAL.Entities;

namespace PaperForge.DAL.Repositories.Interfaces;

public interface IPaperRepository
{
    Task<List<Paper>> GetAllAsync();
    Task<Paper?> GetByIdAsync(Guid id);
    Task<Paper?> GetWithSectionsAsync(Guid id);
    Task<Paper?> GetWithAllAsync(Guid id);
    Task AddAsync(Paper paper);
    Task UpdateAsync(Paper paper);
    Task DeleteAsync(Guid id);
}
