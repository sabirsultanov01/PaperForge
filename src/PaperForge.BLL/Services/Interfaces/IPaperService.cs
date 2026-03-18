using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.BLL.Services.Interfaces;

public interface IPaperService
{
    Task<List<Paper>> GetAllPapersAsync();
    Task<Paper?> GetPaperAsync(Guid id);
    Task<Paper?> GetPaperWithSectionsAsync(Guid id);
    Task<Paper?> GetPaperWithAllAsync(Guid id);
    Task<Paper> CreatePaperAsync(string title, PaperType paperType, CitationStyle citationStyle,
        string? subject = null, string? authorName = null, string? institution = null,
        string? courseName = null, string? instructorName = null, DateTime? deadline = null);
    Task UpdatePaperAsync(Paper paper);
    Task DeletePaperAsync(Guid id);
}
