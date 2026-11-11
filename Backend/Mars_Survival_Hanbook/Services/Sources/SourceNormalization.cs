using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.AspNetCore.WebUtilities;

namespace Mars_Survival_Hanbook.Services.Sources;

public static class SourceNormalization
{
    public const string Version = "source-v1";
    public static string Hash(string text) => Hash(Encoding.UTF8.GetBytes(text));
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public static string CanonicalUrl(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(uri.UserInfo)) throw ApiException.Invalid("Use an HTTP or HTTPS URL without embedded credentials.");
        var b = new UriBuilder(uri) { Fragment = "", Host = uri.IdnHost.ToLowerInvariant() };
        var query = QueryHelpers.ParseQuery(uri.Query)
            .Where(x => !x.Key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase) &&
                !new[] { "fbclid", "gclid" }.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .SelectMany(x => x.Value.Select(v => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(v ?? "")));
        b.Query = string.Join("&", query);
        return b.Uri.AbsoluteUri;
    }
    public static string Identity(SourceProvider provider, string externalId, string? url = null)
    {
        var identity = provider switch
        {
            SourceProvider.Ntrs => "citation:" + externalId,
            SourceProvider.Images => "asset:" + externalId,
            SourceProvider.Pds => "lid:" + externalId.Split("::", StringSplitOptions.None)[0],
            SourceProvider.NasaWeb => "url:" + Hash(CanonicalUrl(url ?? externalId)),
            SourceProvider.MarsTrek => "layer:" + Hash(CanonicalUrl(url!) + "|" + externalId),
            SourceProvider.ManualLegacy => "legacy:" + externalId,
            _ => throw ApiException.Invalid("Invalid source provider.")
        };
        if (identity.Length > 450 || string.IsNullOrWhiteSpace(externalId)) throw ApiException.Invalid("The source identity is empty or too long.");
        return identity;
    }
    public static string CanonicalJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output)) Write(doc.RootElement, writer);
        return Encoding.UTF8.GetString(output.ToArray());
    }
    private static void Write(JsonElement e, Utf8JsonWriter w)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            w.WriteStartObject();
            foreach (var p in e.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                if (p.Name is "took" or "requestId" or "retrievedAt" or "fetchedAt" or "_package_id" ||
                    p.Name.StartsWith("ops:Harvest_Info.", StringComparison.Ordinal) ||
                    p.Name.StartsWith("ops:Tracking_Meta.", StringComparison.Ordinal)) continue;
                w.WritePropertyName(p.Name); Write(p.Value, w);
            }
            w.WriteEndObject();
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            w.WriteStartArray(); foreach (var item in e.EnumerateArray()) Write(item, w); w.WriteEndArray();
        }
        else if (e.ValueKind == JsonValueKind.Number && e.TryGetDecimal(out var d)) w.WriteNumberValue(d);
        else e.WriteTo(w);
    }
    public static (string ContentHash, string Fingerprint) Fingerprint(SourceMetadata metadata, string? providerVersion)
    {
        var hash = Hash(CanonicalJson(DiscoveryJson.Write(metadata)));
        return (hash, Hash(hash + "|" + (providerVersion ?? "") + "|" + Version));
    }
}
