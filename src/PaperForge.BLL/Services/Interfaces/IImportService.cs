using PaperForge.BLL.DTOs;
using PaperForge.DAL.Enums;

namespace PaperForge.BLL.Services.Interfaces;

public interface IImportService
{
    Task<ImportResultDto> ImportFileAsync(
        Stream fileStream,
        string fileName,
        string paperTitle,
        PaperType paperType,
        CitationStyle citationStyle);
}
