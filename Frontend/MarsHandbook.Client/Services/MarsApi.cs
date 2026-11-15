using System.Net.Http.Json;
using System.Text.Json;
using MarsHandbook.Client.Models;
namespace MarsHandbook.Client.Services;

/// <summary>Thin client for the team backend (Render). Parses responses defensively because shapes may evolve.</summary>
public sealed class MarsApi
{
    readonly HttpClient _http;
    public Uri BaseUri { get; }
    public MarsApi(HttpClient http, Uri baseUri) { _http = http; BaseUri = baseUri; }

    public async Task<List<ApiItem>> GetItemsAsync(string path, CancellationToken ct = default)
    {
        using var res = await _http.GetAsync(path, ct);
        res.EnsureSuccessStatusCode();
        var doc = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var arr = doc.ValueKind == JsonValueKind.Array ? doc
                : doc.ValueKind == JsonValueKind.Object && TryGet(doc, "items", out var it) ? it
                : doc.ValueKind == JsonValueKind.Object && TryGet(doc, "data", out var d) ? d : default;
        var list = new List<ApiItem>();
        if (arr.ValueKind != JsonValueKind.Array) return list;
        foreach (var e in arr.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object) continue;
            var title = Str(e, "title") ?? Str(e, "name") ?? "Untitled";
            var sum = Str(e, "summary") ?? Str(e, "description") ?? "";
            list.Add(new ApiItem(title, sum, Str(e, "categorySlug") ?? Str(e, "technologyStatus")));
        }
        return list;
    }

    static bool TryGet(JsonElement o, string name, out JsonElement v)
    {
        foreach (var p in o.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { v = p.Value; return true; }
        v = default; return false;
    }
    static string? Str(JsonElement o, string name) => TryGet(o, name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
