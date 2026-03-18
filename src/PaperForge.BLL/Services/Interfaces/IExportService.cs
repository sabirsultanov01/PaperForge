using PaperForge.BLL.DTOs;

namespace PaperForge.BLL.Services.Interfaces;

public interface IExportService
{
    Task<(byte[] FileBytes, string FileName, string ContentType)>
        ExportPaperAsync(Guid paperId, ExportOptionsDto options);
}
