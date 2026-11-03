using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;
namespace Mars_Survival_Hanbook.Services.Ingestion;

public static class ImportedContentMapper
{
    private static string? Text(JsonElement e, string key) => e.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.ToString() : null;
    public static async Task StageAsync(MarsDbContext db, ScientificSource source, SourceRevision revision, SourceMetadata m, CancellationToken ct)
    {
        if (m.ProviderMetadata is not { } data) return;
        if (source.Provider == SourceProvider.Ntrs && m.ExternalRecordId is { Length: <= 200 } documentId &&
            !await db.ResearchPapers.AnyAsync(x => x.SourceId == source.Id || x.DocumentId == documentId, ct))
        {
            var reportNumber = data.TryGetProperty("otherReportNumbers", out var numbers) && numbers.GetArrayLength() > 0 ? numbers[0].ToString() : null;
            string? pdfUrl = null;
            if (data.TryGetProperty("downloads", out var downloads))
                pdfUrl = downloads.EnumerateArray().Where(x => x.TryGetProperty("links", out _))
                    .Select(x => Text(x.GetProperty("links"), "pdf")).FirstOrDefault(x => x != null);
            db.ResearchPapers.Add(new() { SourceId = source.Id, DocumentId = documentId, Abstract = m.Excerpt,
                ReportNumber = reportNumber is { Length: <= 200 } ? reportNumber : null,
                PdfUrl = pdfUrl == null ? null : new Uri(new Uri("https://ntrs.nasa.gov/"), pdfUrl).AbsoluteUri });
        }
        if (source.Provider == SourceProvider.Images && m.ExternalRecordId is { Length: <= 200 } nasaId &&
            !await db.MediaAssets.AnyAsync(x => x.NasaId == nasaId, ct) && data.TryGetProperty("assets", out var assets))
        {
            var files = assets.GetProperty("collection").GetProperty("items").EnumerateArray()
                .Select(x => Text(x, "href")).OfType<string>().ToArray();
            var original = files.FirstOrDefault(x => x.Contains("~orig.", StringComparison.OrdinalIgnoreCase));
            if (original == null || m.Title.Length > 300) return;
            db.MediaAssets.Add(new() { NasaId = nasaId, SourceId = source.Id, SourceRevisionId = revision.Id,
                Title = m.Title, Url = original, OriginalUrl = original, Credit = m.Credit ?? "", UsageNote = m.UsageNote ?? "",
                AltText = "", FileChecksum = m.AttachmentHashes?.GetValueOrDefault("original"),
                MimeType = Text(data, "originalMimeType"), MediaType = MediaType.Image });
        }
        if (source.Provider == SourceProvider.MarsTrek && !await db.MapLayers.AnyAsync(x => x.SourceId == source.Id, ct) &&
            Text(data, "layerIdentifier") is { } identifier && Text(data, "tileMatrixSet") is { } matrix &&
            Text(data, "format") is { } format && m.Title.Length <= 200)
            db.MapLayers.Add(new() { SourceId = source.Id, SourceRevisionId = revision.Id, Name = m.Title,
                ServiceUrl = m.Url, LayerIdentifier = identifier, TileMatrixSet = matrix, Format = format,
                Attribution = m.Credit ?? m.Organization, Style = Text(data, "style"),
                ResourceUrlTemplate = Text(data, "resourceUrlTemplate"), CoordinateSystem = Text(data, "coordinateSystem"),
                CapabilitiesStoragePath = revision.RawStoragePath });
    }
}
