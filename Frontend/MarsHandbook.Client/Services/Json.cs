using System.Text.Json;
namespace MarsHandbook.Client.Services;

/// <summary>Case-insensitive, never-throwing accessors for loosely typed API JSON.</summary>
public static class J
{
    public static JsonElement Get(this JsonElement o, string name)
    {
        if (o.ValueKind == JsonValueKind.Object)
            foreach (var p in o.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
        return default;
    }
    public static string? S(this JsonElement o, string name)
    {
        var v = o.Get(name);
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }
    public static int I(this JsonElement o, string name)
    {
        var v = o.Get(name);
        return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;
    }
    public static List<JsonElement> A(this JsonElement o, string name)
    {
        var v = o.Get(name);
        return v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().ToList() : new List<JsonElement>();
    }
    public static string Show(this JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString() ?? "",
        JsonValueKind.Number => v.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => ""
    };
}
