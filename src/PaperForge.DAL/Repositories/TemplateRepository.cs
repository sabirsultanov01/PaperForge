using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;
using PaperForge.DAL.Repositories.Interfaces;
using PaperForge.DAL.Seed;

namespace PaperForge.DAL.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private static readonly List<Template> _templates = TemplateSeeder.GetTemplates();

    public Task<Template?> GetByPaperTypeAsync(PaperType paperType)
    {
        var template = _templates.FirstOrDefault(t => t.PaperType == paperType);
        return Task.FromResult(template);
    }

    public Task<List<Template>> GetAllAsync()
        => Task.FromResult(_templates.ToList());
}
