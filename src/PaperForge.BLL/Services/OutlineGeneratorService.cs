using System.Text.Json;
using PaperForge.BLL.DTOs;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;

namespace PaperForge.BLL.Services;

public class OutlineGeneratorService : IOutlineGeneratorService
{
    private readonly ITemplateRepository _templateRepo;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OutlineGeneratorService(ITemplateRepository templateRepo)
        => _templateRepo = templateRepo;

    public async Task<List<SectionTemplateDto>> GenerateOutlineAsync(PaperType paperType)
    {
        var template = await _templateRepo.GetByPaperTypeAsync(paperType);
        if (template is not null)
        {
            var sections = JsonSerializer.Deserialize<List<SectionTemplateDto>>(
                template.StructureJson, JsonOptions);
            if (sections is not null && sections.Count > 0)
                return sections;
        }

        return
        [
            new() { Title = "Introduction", WordTarget = 300, Guidance = "Introduce your topic and state your thesis." },
            new() { Title = "Body", WordTarget = 600, Guidance = "Present your main arguments with supporting evidence." },
            new() { Title = "Conclusion", WordTarget = 250, Guidance = "Summarize key points and restate your thesis." }
        ];
    }
}
