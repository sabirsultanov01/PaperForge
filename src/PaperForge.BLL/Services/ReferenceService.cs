using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.BLL.Services;

public class ReferenceService : IReferenceService
{
    private readonly IReferenceRepository _refRepo;

    public ReferenceService(IReferenceRepository refRepo) => _refRepo = refRepo;

    public Task<List<Reference>> GetReferencesAsync(Guid paperId)
        => _refRepo.GetByPaperIdAsync(paperId);

    public async Task<Reference> AddReferenceAsync(Guid paperId, Reference reference)
    {
        reference.Id = Guid.NewGuid();
        reference.PaperId = paperId;
        await _refRepo.AddAsync(reference);
        return reference;
    }

    public Task DeleteReferenceAsync(Guid id) => _refRepo.DeleteAsync(id);
}
