using System.Text.Json;
using Microsoft.JSInterop;
namespace MarsHandbook.Client.Services;

public sealed record Cat(string Slug, string Name, string Desc, int Count, string? Image, string? Credit);
public sealed record Mod(string Slug, string Name, string Desc, int Count, List<Cat> Cats);
public sealed record Art(string Module, string Slug, string Title, string Summary, string Cat, string Date);
public sealed record Bm(string M, string S, string T);

/// <summary>Reads the five handbook modules, their topics and articles from the team backend; keeps bookmarks in the browser.</summary>
public sealed class HandbookService
{
    public static readonly (string Slug, string Label)[] Order =
    {
        ("discover-mars", "Discover Mars"),
        ("survival-knowledge", "Survival Knowledge"),
        ("living-on-mars", "Living on Mars"),
        ("science-technology", "Science & Technology"),
        ("human-survival", "Human Survival"),
    };

    readonly MarsApi _api; readonly IJSRuntime _js;
    List<Mod>? _mods; List<Art>? _arts;
    public HandbookService(MarsApi api, IJSRuntime js) { _api = api; _js = js; }

    public static string Label(string slug) => Order.FirstOrDefault(o => o.Slug == slug).Label ?? slug;
    public static bool Known(string slug) => Order.Any(o => o.Slug == slug);

    readonly Dictionary<string, Task<Mod>> _modules = new();

    public async Task<Mod> ModuleAsync(string slug)
    {
        if (!Known(slug)) throw new InvalidOperationException("Unknown module.");
        if (!_modules.TryGetValue(slug, out var task))
            _modules[slug] = task = ReadModuleAsync(slug);
        try { return await task; }
        catch
        {
            if (_modules.GetValueOrDefault(slug) == task) _modules.Remove(slug);
            throw;
        }
    }

    async Task<Mod> ReadModuleAsync(string slug)
    {
        var j = await _api.GetJsonAsync($"api/{slug}?language=en");
        var cats = j.A("categories").Select(c => new Cat(
            c.S("slug") ?? "", c.S("name") ?? "", c.S("description") ?? "",
            c.I("publishedArticleCount"), c.S("imageUrl"), c.S("imageCredit"))).ToList();
        return new Mod(slug, j.S("name") ?? Label(slug), j.S("description") ?? "", j.I("publishedArticleCount"), cats);
    }

    public async Task<List<Mod>> ModulesAsync()
    {
        if (_mods is not null) return _mods;
        return _mods = (await Task.WhenAll(Order.Select(o => ModuleAsync(o.Slug)))).ToList();
    }

    public async Task<List<Art>> ArticlesAsync(string module)
    {
        if (!Known(module)) return new List<Art>();
        var path = module == "science-technology"
            ? "api/science-technology/technologies?language=en"
            : $"api/{module}/articles?language=en";
        var j = await _api.GetJsonAsync(path);
        var items = j.ValueKind == JsonValueKind.Array ? j.EnumerateArray().ToList() : j.A("items");
        return items.Select(e => new Art(module, e.S("slug") ?? "", e.S("title") ?? "Untitled", e.S("summary") ?? "",
            e.S("categorySlug") ?? "", (e.S("reviewedAt") ?? e.S("publishedAt") ?? "").Split('T')[0])).ToList();
    }

    public async Task<List<Art>> AllArticlesAsync()
    {
        if (_arts is not null) return _arts;
        var res = await Task.WhenAll(Order.Select(o => ArticlesAsync(o.Slug)));
        return _arts = res.SelectMany(x => x).ToList();
    }

    public Task<JsonElement> DetailAsync(string module, string slug)
    {
        if (!Known(module)) throw new InvalidOperationException("Unknown module.");
        var s = Uri.EscapeDataString(slug);
        return _api.GetJsonAsync(module == "science-technology"
            ? $"api/science-technology/technologies/{s}?language=en"
            : $"api/{module}/articles/{s}?language=en");
    }

    public string Abs(string path) => new Uri(_api.BaseUri, path).ToString();

    // ---- bookmarks (browser localStorage, tab-separated lines) ----
    public async Task<List<Bm>> BookmarksAsync()
    {
        try
        {
            var s = await _js.InvokeAsync<string>("mhui.bmGet");
            return (s ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Split('\t'))
                .Where(p => p.Length == 3).Select(p => new Bm(p[0], p[1], p[2])).ToList();
        }
        catch { return new List<Bm>(); }
    }

    public async Task<bool> ToggleAsync(string m, string s, string t)
    {
        var l = await BookmarksAsync();
        var ex = l.FirstOrDefault(x => x.M == m && x.S == s);
        bool on;
        if (ex is not null) { l.Remove(ex); on = false; }
        else { l.Add(new Bm(m, s, t.Replace('\t', ' ').Replace('\n', ' '))); on = true; }
        try { await _js.InvokeVoidAsync("mhui.bmSet", string.Join('\n', l.Select(x => $"{x.M}\t{x.S}\t{x.T}"))); } catch { }
        return on;
    }
}
