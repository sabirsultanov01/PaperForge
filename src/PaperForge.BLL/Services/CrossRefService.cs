using System.Text.Json;
using PaperForge.BLL.DTOs;
using PaperForge.BLL.Services.Interfaces;

namespace PaperForge.BLL.Services;

public class CrossRefService : ICrossRefService
{
    private readonly HttpClient _httpClient;

    public CrossRefService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.crossref.org/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PaperForge/1.0 (mailto:paperforge@example.com)");
    }

    public async Task<CrossRefResultDto> LookupDoiAsync(string doi)
    {
        try
        {
            var cleanDoi = doi.Trim().Replace("https://doi.org/", "").Replace("http://doi.org/", "");
            var response = await _httpClient.GetAsync($"works/{Uri.EscapeDataString(cleanDoi)}");

            if (!response.IsSuccessStatusCode)
                return new CrossRefResultDto { Success = false, ErrorMessage = "DOI not found." };

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("message");

            var result = new CrossRefResultDto { Success = true, DOI = cleanDoi };

            if (message.TryGetProperty("title", out var titles) && titles.GetArrayLength() > 0)
                result.Title = titles[0].GetString() ?? "";

            if (message.TryGetProperty("author", out var authors) && authors.GetArrayLength() > 0)
            {
                var first = authors[0];
                result.AuthorLastName = first.TryGetProperty("family", out var f) ? f.GetString() ?? "" : "";
                result.AuthorFirstName = first.TryGetProperty("given", out var g) ? g.GetString() ?? "" : "";
            }

            if (message.TryGetProperty("published-print", out var pub)
                && pub.TryGetProperty("date-parts", out var parts)
                && parts.GetArrayLength() > 0
                && parts[0].GetArrayLength() > 0)
            {
                result.Year = parts[0][0].GetInt32();
            }
            else if (message.TryGetProperty("published-online", out var pubOnline)
                && pubOnline.TryGetProperty("date-parts", out var partsOnline)
                && partsOnline.GetArrayLength() > 0
                && partsOnline[0].GetArrayLength() > 0)
            {
                result.Year = partsOnline[0][0].GetInt32();
            }

            if (message.TryGetProperty("container-title", out var journal) && journal.GetArrayLength() > 0)
                result.Journal = journal[0].GetString();

            if (message.TryGetProperty("volume", out var vol))
                result.Volume = vol.GetString();

            if (message.TryGetProperty("issue", out var issue))
                result.Issue = issue.GetString();

            if (message.TryGetProperty("page", out var pages))
                result.Pages = pages.GetString();

            if (message.TryGetProperty("publisher", out var publisher))
                result.Publisher = publisher.GetString();

            return result;
        }
        catch (Exception ex)
        {
            return new CrossRefResultDto { Success = false, ErrorMessage = $"Lookup failed: {ex.Message}" };
        }
    }
}
