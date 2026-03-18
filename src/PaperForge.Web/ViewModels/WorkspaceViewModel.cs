using PaperForge.DAL.Entities;

namespace PaperForge.Web.ViewModels;

public class WorkspaceViewModel
{
    public Paper Paper { get; set; } = null!;
    public List<Section> Sections { get; set; } = [];
    public List<Reference> References { get; set; } = [];
}
