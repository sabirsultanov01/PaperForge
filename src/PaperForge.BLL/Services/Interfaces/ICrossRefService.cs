using PaperForge.BLL.DTOs;

namespace PaperForge.BLL.Services.Interfaces;

public interface ICrossRefService
{
    Task<CrossRefResultDto> LookupDoiAsync(string doi);
}
