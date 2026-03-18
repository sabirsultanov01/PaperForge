using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.BLL.Services.Interfaces;

public interface IReferenceService
{
    Task<List<Reference>> GetReferencesAsync(Guid paperId);
    Task<Reference> AddReferenceAsync(Guid paperId, Reference reference);
    Task DeleteReferenceAsync(Guid id);
}
