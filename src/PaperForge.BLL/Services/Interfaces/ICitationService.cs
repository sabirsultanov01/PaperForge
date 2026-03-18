using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.BLL.Services.Interfaces;

public interface ICitationService
{
    string FormatInTextCitation(Reference reference, CitationStyle style);
    string FormatBibliographyEntry(Reference reference, CitationStyle style);
    List<string> FormatBibliography(IEnumerable<Reference> references, CitationStyle style);
}
