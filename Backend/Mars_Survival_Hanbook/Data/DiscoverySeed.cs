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
        await SeedModuleAsync(db, "discover-mars", "Discover Mars", "Khám phá Sao Hỏa từ các nguồn khoa học được kiểm chứng.",
            Categories.Select(c => (c.Slug, c.Name, (string?)null)).ToArray(), ct);
        await SeedModuleAsync(db, "survival-knowledge", "Survival Knowledge", "Conditions for living on Mars, explained through NASA observations and research.", [
            ("oxygen-and-low-pressure", "Oxygen and low pressure", "Atmospheric composition, oxygen abundance and surface pressure."),
            ("extreme-temperatures", "Extreme temperatures", "Temperature ranges by location, time and scale of observation."),
            ("space-radiation", "Space radiation", "Radiation sources and absorbed dose rates measured on Mars."),
            ("dust-storms", "Dust storms", "How airborne dust affects sunlight and solar power."),
            ("partial-gravity", "Partial gravity and the human body", "Surface gravity and evidence about long-term human health.")
        ], ct);
        await SeedModuleAsync(db, "living-on-mars", "Living on Mars", "Plan a surface outpost and compare resource use across sols.", [
            ("housing", "Housing", "Construction, crew time and habitat capacity."),
            ("water-and-oxygen", "Water and oxygen", "Water recovery and oxygen production."),
            ("food", "Food", "Rations, crop inputs and harvests."),
            ("energy", "Energy", "Solar generation, battery storage and power priorities."),
            ("sanitation-and-waste", "Sanitation and waste", "Hygiene water, collection and tank overflow."),
            ("community", "Community life", "Crew hours and shared activities.")
        ], ct);
        foreach (var (symbol, quantity, si) in new[] { ("K", "Temperature", true), ("°C", "Temperature", false),
            ("µGy/day", "AbsorbedDoseRate", false), ("1", "OpticalDepth", true), ("%", "VolumeFraction", false), ("Pa", "Pressure", true), ("hPa", "Pressure", false), ("m/s²", "Acceleration", true), ("m", "Length", true), ("km", "Length", false) })
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
    private static async Task SeedModuleAsync(MarsDbContext db, string moduleSlug, string moduleName,
        string description, (string Slug, string Name, string? Description)[] categories, CancellationToken ct)
    {
        var module = await db.Modules.SingleOrDefaultAsync(x => x.Slug == moduleSlug, ct);
        if (module == null)
        {
            module = new() { Name = moduleName, Slug = moduleSlug, Description = description };
            db.Modules.Add(module); await db.SaveChangesAsync(ct);
        }
        if (moduleSlug is "survival-knowledge" or "living-on-mars")
        {
            module.Name = moduleName;
            module.Description = description;
        }
        for (var i = 0; i < categories.Length; i++)
        {
            var (slug, name, topicDescription) = categories[i];
            var category = await db.Categories.SingleOrDefaultAsync(x => x.ModuleId == module.Id && x.Slug == slug, ct);
            if (category == null)
                db.Categories.Add(new() { ModuleId = module.Id, Name = name, Slug = slug, Description = topicDescription, DisplayOrder = i });
            else if (moduleSlug is "survival-knowledge" or "living-on-mars")
            {
                if (moduleSlug == "living-on-mars") category.DisplayOrder = i;
                category.Name = name;
                category.Description = topicDescription;
            }
        }
    }
}
