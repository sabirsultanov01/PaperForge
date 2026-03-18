using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.DAL.Repositories.Interfaces;

public interface ITemplateRepository
{
    Task<Template?> GetByPaperTypeAsync(PaperType paperType);
    Task<List<Template>> GetAllAsync();
}
