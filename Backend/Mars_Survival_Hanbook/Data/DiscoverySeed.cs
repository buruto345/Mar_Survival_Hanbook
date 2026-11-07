using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;
namespace Mars_Survival_Hanbook.Data;

public static class DiscoverySeed
{
    public static readonly (string Slug, string Name)[] Categories = [
        ("exploration-history", "Lịch sử khám phá Sao Hỏa"),
        ("structure-and-terrain", "Cấu tạo và địa hình"),
        ("atmosphere-temperature-gravity", "Khí quyển, nhiệt độ, trọng lực"),
        ("water-and-life", "Nước và dấu hiệu sự sống"),
        ("missions-and-rovers", "Sứ mệnh NASA và tàu thăm dò")
    ];
    public static async Task RunAsync(MarsDbContext db, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "discovery-seed", ct);
        var module = await db.Modules.SingleOrDefaultAsync(x => x.Slug == "discover-mars", ct);
        if (module == null)
        {
            module = new() { Name = "Discover Mars", Slug = "discover-mars", Description = "Khám phá Sao Hỏa từ các nguồn khoa học được kiểm chứng." };
            db.Modules.Add(module); await db.SaveChangesAsync(ct);
        }
        for (var i = 0; i < Categories.Length; i++)
        {
            var (slug, name) = Categories[i];
            if (!await db.Categories.AnyAsync(x => x.ModuleId == module.Id && x.Slug == slug, ct))
                db.Categories.Add(new() { ModuleId = module.Id, Name = name, Slug = slug, DisplayOrder = i });
        }
        foreach (var (symbol, quantity, si) in new[] { ("K", "Temperature", true), ("°C", "Temperature", false),
            ("Pa", "Pressure", true), ("hPa", "Pressure", false), ("m/s²", "Acceleration", true), ("m", "Length", true), ("km", "Length", false) })
            if (!await db.Units.AnyAsync(x => x.Symbol == symbol && x.QuantityKind == quantity, ct))
                db.Units.Add(new() { Name = symbol, Symbol = symbol, QuantityKind = quantity, IsSi = si });
        foreach (var (key, type, url) in new[] {
            ("images", ConnectorType.Images, "https://images-api.nasa.gov/"),
            ("ntrs", ConnectorType.Ntrs, "https://ntrs.nasa.gov/"),
            ("pds", ConnectorType.Pds, "https://pds.nasa.gov/api/search/1/"),
            ("mars-trek", ConnectorType.MarsTrek, "https://trek.nasa.gov/"),
            ("manual-archive", ConnectorType.ManualArchive, "https://science.nasa.gov/") })
            if (!await db.NasaConnectors.AnyAsync(x => x.Key == key, ct))
                db.NasaConnectors.Add(new() { Key = key, Name = key, ConnectorType = type, BaseUrl = url, IsEnabled = true,
                    RateLimitPerMinute = 60, AccessPolicy = "NASA or NASA coauthored/cooperated sources; preserve affiliations and credit; reviewer verifies each revision." });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
