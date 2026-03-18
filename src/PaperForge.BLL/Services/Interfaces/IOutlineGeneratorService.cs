using PaperForge.BLL.DTOs;
using PaperForge.DAL.Enums;

namespace PaperForge.BLL.Services.Interfaces;

public interface IOutlineGeneratorService
{
    Task<List<SectionTemplateDto>> GenerateOutlineAsync(PaperType paperType);
}
