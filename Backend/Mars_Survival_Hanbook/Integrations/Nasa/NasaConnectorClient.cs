using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;
using Mars_Survival_Hanbook.Services;

namespace Mars_Survival_Hanbook.Integrations.Nasa;

public record DiscoveryPage(string[] Keys, string? NextCheckpoint);
public sealed class NasaConnectorClient(SafeNasaHttp http, RawStorage storage, IConfiguration config)
{
    public static SourceProvider Provider(ConnectorType type) => type switch {
        ConnectorType.Images => SourceProvider.Images, ConnectorType.Ntrs => SourceProvider.Ntrs,
        ConnectorType.Pds => SourceProvider.Pds, ConnectorType.MarsTrek => SourceProvider.MarsTrek,
        ConnectorType.ManualArchive => SourceProvider.NasaWeb, _ => throw ApiException.Invalid("Unsupported connector.")
    };
    private static Uri Base(NasaConnector connector) => new(connector.BaseUrl.TrimEnd('/') + "/");
    private static string Esc(string value) => Uri.EscapeDataString(value);
    // NASA's asset API still emits HTTP links although its asset host supports HTTPS.
    // Upgrade only that exact legacy host; all other URLs still pass the normal allowlist.
    public static string SecureImageAssetUrl(string value)
    {
        var uri = new Uri(value);
        return uri.Scheme == "http" && uri.IdnHost.Equals("images-assets.nasa.gov", StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo)
            ? new UriBuilder(uri) { Scheme = "https", Port = -1 }.Uri.AbsoluteUri : value;
    }
    private async Task<JsonElement> JsonAsync(Uri url, CancellationToken ct)
    {
        var response = await http.GetAsync(url, ct);
        if (!response.MimeType.Contains("json")) throw new UpstreamException("invalid_content_type", false);
        try { return JsonDocument.Parse(response.Bytes).RootElement.Clone(); }
        catch (JsonException) { throw new UpstreamException("invalid_schema", false); }
    }
    private static JsonElement Required(JsonElement e, string key, JsonValueKind kind)
    {
        if (!e.TryGetProperty(key, out var p) || p.ValueKind != kind) throw new UpstreamException("invalid_schema", false);
        return p;
    }
    private static string? S(JsonElement e, string key) => e.TryGetProperty(key, out var p) && p.ValueKind != JsonValueKind.Null ? p.ToString() : null;
    public async Task<DiscoveryPage> DiscoverAsync(NasaConnector c, ImportRequest r, string? checkpoint, CancellationToken ct)
    {
        if (c.ConnectorType == ConnectorType.Pds && r.IncludeMembers) return await DiscoverPdsAsync(c, r, checkpoint, ct);
        if (r.RecordIds.Length > 0) return new(r.RecordIds.Distinct(StringComparer.Ordinal).Take(r.MaxItems).ToArray(), null);
        if (c.ConnectorType == ConnectorType.MarsTrek) return new([r.LayerKey!], null);
        var page = int.TryParse(checkpoint, out var n) ? n : 0;
        if (c.ConnectorType == ConnectorType.Images)
        {
            var data = await JsonAsync(new(Base(c), $"search?q={Esc(r.Query!)}&media_type=image&page_size={r.PageSize}&page={page + 1}"), ct);
            var collection = Required(data, "collection", JsonValueKind.Object);
            var items = Required(collection, "items", JsonValueKind.Array).EnumerateArray().ToArray();
            var keys = items.Select((item, i) => item.TryGetProperty("data", out var d) && d.GetArrayLength() > 0
                ? S(d[0], "nasa_id") ?? $"missing:{page}:{i}" : $"missing:{page}:{i}").ToArray();
            var hasNext = collection.TryGetProperty("links", out var links) && links.EnumerateArray().Any(x => S(x, "rel") == "next");
            return new(keys, hasNext ? (page + 1).ToString() : null);
        }
        if (c.ConnectorType == ConnectorType.Ntrs)
        {
            var data = await JsonAsync(new(Base(c), $"api/citations/search?q={Esc(r.Query!)}&distribution=PUBLIC&page.size={r.PageSize}&page.from={page}"), ct);
            var rows = Required(data, "results", JsonValueKind.Array).EnumerateArray().ToArray();
            var keys = rows.Select((x, i) => S(x, "id") ?? $"missing:{page}:{i}").ToArray();
            var total = data.TryGetProperty("stats", out var stats) && stats.TryGetProperty("total", out var count) ? count.GetInt32() : page + rows.Length;
            return new(keys, page + rows.Length < total && rows.Length > 0 ? (page + rows.Length).ToString() : null);
        }
        throw ApiException.Invalid("This connector requires explicit recordIds.");
    }
    public async Task<SourceCandidate> FetchAsync(NasaConnector c, string key, CancellationToken ct)
    {
        var blobs = new List<object>();
        var hashes = new Dictionary<string, string>();
        async Task<NasaResponse> Fetch(Uri uri, string name)
        {
            var response = await http.GetAsync(uri, ct);
            var saved = await storage.PutAsync(response.Bytes, ct, response.MimeType);
            blobs.Add(new { Name = name, Url = response.Url.AbsoluteUri, response.MimeType, Blob = saved.Path, Sha256 = saved.Hash });
            hashes[name] = saved.Hash; return response;
        }
        async Task<JsonElement> FetchJson(Uri uri, string name)
        {
            var response = await Fetch(uri, name);
            if (!response.MimeType.Contains("json")) throw new UpstreamException("invalid_content_type", false);
            return JsonDocument.Parse(response.Bytes).RootElement.Clone();
        }
        SourceMetadata metadata;
        string? providerVersion = null;
        if (key.StartsWith("missing:", StringComparison.Ordinal)) throw new UpstreamException("missing_provider_id", false);
        switch (c.ConnectorType)
        {
            case ConnectorType.Ntrs:
            {
                var d = await FetchJson(new(Base(c), "api/citations/" + Esc(key)), "metadata");
                if (Required(d, "distribution", JsonValueKind.String).GetString() != "PUBLIC") throw new UpstreamException("policy_not_public", false);
                var title = S(d, "title") ?? throw new UpstreamException("missing_title", false);
                if (!MarsRelevant(title + " " + S(d, "abstract"))) throw new UpstreamException("policy_not_mars", false);
                var authors = new List<SourceAuthor>();
                if (d.TryGetProperty("authorAffiliations", out var affiliations))
                    foreach (var a in affiliations.EnumerateArray())
                        if (a.TryGetProperty("meta", out var m))
                            authors.Add(new(m.TryGetProperty("author", out var author) ? S(author, "name") ?? "" : "",
                                m.TryGetProperty("organization", out var org) ? S(org, "name") ?? "" : ""));
                var organization = d.TryGetProperty("center", out var center) ? S(center, "name") ?? "" : "";
                if (d.TryGetProperty("downloads", out var downloads))
                    foreach (var (file, index) in downloads.EnumerateArray().Take(5).Select((v, i) => (v, i)))
                        if (file.TryGetProperty("links", out var links) && S(links, "pdf") is { } pdf)
                        {
                            var response = await Fetch(new(Base(c), pdf), "pdf-" + index);
                            if (!response.Bytes.AsSpan().StartsWith("%PDF-"u8)) throw new UpstreamException("invalid_pdf", false);
                        }
                hashes.Remove("metadata");
                // The search index name changes when NASA reindexes, independently of the document.
                var documentMetadata = JsonNode.Parse(d.GetRawText())!.AsObject();
                documentMetadata.Remove("index");
                var publicationDate = d.TryGetProperty("publications", out var publications) && publications.GetArrayLength() > 0
                    ? S(publications[0], "publicationDate") : null;
                metadata = new(title, $"https://ntrs.nasa.gov/citations/{Esc(key)}", authors.ToArray(), organization,
                    SourceType.TechnicalReport, S(d, "abstract"), "Reviewer must verify document access and usage terms.",
                    Doi: S(d, "doi"), ExternalRecordId: key,
                    PublishedAt: DateTimeOffset.TryParse(publicationDate, out var published) ? published : null,
                    ProviderMetadata: JsonSerializer.SerializeToElement(documentMetadata), AttachmentHashes: hashes);
                break;
            }
            case ConnectorType.Images:
            {
                var search = await FetchJson(new(Base(c), "search?nasa_id=" + Esc(key)), "search");
                var items = Required(Required(search, "collection", JsonValueKind.Object), "items", JsonValueKind.Array);
                if (items.GetArrayLength() == 0) throw new UpstreamException("upstream_404", false);
                var item = items[0]; var d = Required(item, "data", JsonValueKind.Array)[0];
                var title = S(d, "title") ?? throw new UpstreamException("missing_title", false);
                if (!MarsRelevant(title + " " + S(d, "description") + " " + S(d, "keywords"))) throw new UpstreamException("policy_not_mars", false);
                var assets = await FetchJson(new(Base(c), "asset/" + Esc(key)), "assets");
                var location = await FetchJson(new(Base(c), "metadata/" + Esc(key)), "metadata-location");
                var detail = await FetchJson(new Uri(SecureImageAssetUrl(S(location, "location") ?? throw new UpstreamException("missing_metadata", false))), "metadata");
                var credit = S(detail, "XMP:Credit") ?? S(d, "secondary_creator") ?? "";
                var files = Required(Required(assets, "collection", JsonValueKind.Object), "items", JsonValueKind.Array)
                    .EnumerateArray().Select(x => S(x, "href")).OfType<string>().ToArray();
                var original = files.FirstOrDefault(x => x.Contains("~orig.", StringComparison.OrdinalIgnoreCase));
                string? originalMimeType = null;
                if (original != null)
                {
                    var response = await Fetch(new Uri(SecureImageAssetUrl(original)), "original");
                    if (!response.MimeType.StartsWith("image/")) throw new UpstreamException("invalid_image", false);
                    originalMimeType = response.MimeType;
                }
                var provider = JsonSerializer.SerializeToElement(new { Data = d, Assets = assets, Metadata = detail, OriginalMimeType = originalMimeType }, DiscoveryJson.Options);
                hashes = hashes.Where(x => x.Key == "original").ToDictionary(x => x.Key, x => x.Value);
                metadata = new(title, "https://images.nasa.gov/details/" + Esc(key),
                    [new(S(d, "photographer") ?? credit, S(d, "center") ?? "")], S(d, "center") ?? "",
                    SourceType.Media, S(d, "description"), S(detail, "XMP:UsageTerms") ?? "Reviewer must verify media rights.",
                    credit, ExternalRecordId: key, ProviderMetadata: provider, AttachmentHashes: hashes);
                break;
            }
            case ConnectorType.Pds:
            {
                var d = await FetchJson(new(Base(c), "products/" + Esc(key)), "registry");
                var lidvid = S(d, "id") ?? throw new UpstreamException("missing_lidvid", false);
                if (!(S(d, "targets") ?? "").Contains("planet.mars", StringComparison.OrdinalIgnoreCase)) throw new UpstreamException("policy_not_mars", false);
                var m = Required(d, "metadata", JsonValueKind.Object);
                var label = await Fetch(new Uri(S(m, "label_url") ?? throw new UpstreamException("missing_label", false)), "label");
                var document = ParseXml(label.Bytes);
                providerVersion = S(m, "version") ?? lidvid.Split("::").LastOrDefault();
                PdsPilotResult? samples = null;
                if (S(d, "type") == "Product_Observational" && lidvid.Contains("mars2020_meda:data_calibrated_env:", StringComparison.Ordinal))
                {
                    var file = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "File_Area_Observational")?
                        .Descendants().FirstOrDefault(x => x.Name.LocalName == "file_name")?.Value;
                    if (file != null)
                    {
                        var dataFile = await Fetch(new Uri(label.Url, file), "data");
                        samples = PdsMedaParser.Parse(document, dataFile.Bytes, config.GetValue("Nasa:Pds:MaxSamples", 100));
                    }
                }
                var normalized = JsonSerializer.SerializeToElement(new { Registry = d,
                    Label = document.ToString(SaveOptions.DisableFormatting), Samples = samples,
                    Note = "Candidate samples remain in staging. SCLK/LMST are retained, never converted to UTC without a verified clock solution." }, DiscoveryJson.Options);
                hashes.Remove("registry");
                metadata = new(S(d, "title") ?? lidvid, "https://pds.nasa.gov/api/search/1/products/" + Esc(lidvid), [],
                    "NASA Planetary Data System", SourceType.Dataset, ProviderMetadata: normalized, ExternalRecordId: lidvid, AttachmentHashes: hashes);
                break;
            }
            case ConnectorType.MarsTrek:
            {
                var path = config["Nasa:MarsTrek:Layers:" + key] ?? throw ApiException.Invalid("The layer key has not been configured.");
                var response = await Fetch(new(Base(c), path), "capabilities");
                var doc = ParseXml(response.Bytes);
                var layer = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Layer") ?? throw new UpstreamException("invalid_wmts", false);
                string? Element(string name) => layer.Descendants().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
                var identifier = layer.Elements().FirstOrDefault(x => x.Name.LocalName == "Identifier")?.Value ?? key;
                var provider = JsonSerializer.SerializeToElement(new {
                    LayerIdentifier = identifier, Style = layer.Descendants().FirstOrDefault(x => x.Name.LocalName == "Style")?.Elements().FirstOrDefault(x => x.Name.LocalName == "Identifier")?.Value,
                    TileMatrixSet = Element("TileMatrixSet"), Format = Element("Format"),
                    ResourceUrlTemplate = layer.Descendants().FirstOrDefault(x => x.Name.LocalName == "ResourceURL")?.Attribute("template")?.Value,
                    CoordinateSystem = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "SupportedCRS")?.Value,
                    BoundingBoxes = layer.Descendants().Where(x => x.Name.LocalName.Contains("BoundingBox")).Select(x => x.ToString()).ToArray()
                }, DiscoveryJson.Options);
                metadata = new(Element("Title") ?? identifier, response.Url.AbsoluteUri, [new("NASA Solar System Treks", "NASA/JPL")],
                    "NASA/JPL", SourceType.MapLayer, Credit: "NASA/JPL", ExternalRecordId: identifier, ProviderMetadata: provider);
                key = identifier; break;
            }
            default: throw ApiException.Invalid("Register web sources through /sources/manual.");
        }
        var manifest = await storage.PutAsync(Encoding.UTF8.GetBytes(DiscoveryJson.Write(new { SchemaVersion = 1, Files = blobs })), ct);
        return new(Provider(c.ConnectorType), key, providerVersion, metadata, manifest.Path, manifest.Hash, DateTimeOffset.UtcNow);
    }
    public static bool MarsRelevant(string text) =>
        !text.Contains("Pennsylvania", StringComparison.OrdinalIgnoreCase) &&
        !text.Contains("festival", StringComparison.OrdinalIgnoreCase) &&
        new[] { "Mars", "Martian", "Perseverance", "Curiosity", "Jezero", "Olympus Mons", "Valles Marineris" }.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));
    private async Task<DiscoveryPage> DiscoverPdsAsync(NasaConnector c, ImportRequest r, string? checkpoint, CancellationToken ct)
    {
        var state = checkpoint == null ? new PdsCheckpoint { Pending = r.RecordIds.Select(x => new PdsCursor(x, false, null, null)).ToList() }
            : DiscoveryJson.Read<PdsCheckpoint>(checkpoint);
        if (state.Pending.Count == 0) return new([], null);
        if (++state.Pages > 1000) throw new UpstreamException("pds_scan_limit", false);
        var current = state.Pending[0]; state.Pending.RemoveAt(0);
        var keys = new List<string>();
        if (!current.Members)
        {
            var record = await JsonAsync(new(Base(c), "products/" + Esc(current.Id)), ct);
            var pinned = S(record, "id") ?? throw new UpstreamException("missing_lidvid", false);
            if (state.Seen.Add(pinned))
            {
                keys.Add(pinned);
                if (S(record, "type") is "Product_Bundle" or "Product_Collection")
                    state.Pending.Add(new(pinned, true, null, S(record, "type")));
            }
        }
        else
        {
            var path = "products/" + Esc(current.Id) + "/members?sort=lidvid&limit=" + r.PageSize;
            if (current.After != null) path += "&search-after=" + Esc(current.After);
            if (current.Type == "Product_Collection" && r.From.HasValue && r.To.HasValue)
            {
                var filter = $"((pds:Time_Coordinates.pds:start_date_time le \"{r.To.Value:O}\") and (pds:Time_Coordinates.pds:stop_date_time ge \"{r.From.Value:O}\"))";
                path += "&q=" + Esc(filter);
            }
            var page = await JsonAsync(new(Base(c), path), ct);
            var rows = Required(page, "data", JsonValueKind.Array).EnumerateArray().ToArray();
            foreach (var row in rows)
            {
                var id = S(row, "id") ?? throw new UpstreamException("missing_lidvid", false);
                var type = S(row, "type");
                if (type == "Product_Collection")
                {
                    if (!id.Contains(":" + r.PdsCollection + "::", StringComparison.Ordinal)) continue;
                    if (state.Seen.Add(id)) { keys.Add(id); state.Pending.Add(new(id, true, null, type)); }
                }
                else if (type == "Product_Observational" && state.Seen.Add(id)) keys.Add(id);
            }
            if (rows.Length == r.PageSize)
            {
                var last = S(rows[^1], "id")!;
                if (last == current.After) throw new UpstreamException("pds_cursor_not_advancing", false);
                state.Pending.Insert(0, current with { After = last });
            }
        }
        return new(keys.ToArray(), state.Pending.Count == 0 ? null : DiscoveryJson.Write(state));
    }
    public static XDocument ParseXml(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20_000_000 });
        return XDocument.Load(reader);
    }
}

public record PdsCursor(string Id, bool Members, string? After, string? Type);
public class PdsCheckpoint
{
    public List<PdsCursor> Pending { get; set; } = [];
    public HashSet<string> Seen { get; set; } = [];
    public int Pages { get; set; }
}
