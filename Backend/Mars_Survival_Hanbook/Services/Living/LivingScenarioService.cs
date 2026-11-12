using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Living;

public sealed class LivingScenarioService(MarsDbContext db)
{
    private const string ReviewedEditionHash = "bfea6004e4a07df9b8ebdc1044cc7edfef70c441888116b5ce59182141804293";

    public static ScenarioPack Baseline()
    {
        using var stream = typeof(LivingScenarioService).Assembly.GetManifestResourceStream(
            "Mars_Survival_Hanbook.Data.Living.outpost.en.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd().ReplaceLineEndings("\n").TrimEnd();
        if (Sources.SourceNormalization.Hash(json) != ReviewedEditionHash)
            throw ApiException.Gate("The bundled scenario has changed and requires English review.");
        return DiscoveryJson.Read<ScenarioPack>(json);
    }

    private IQueryable<LivingScenarioVersion> Available => db.LivingScenarios.AsNoTracking().Where(v =>
        v.ArchivedAt == null && !v.Parameters.Any(p => p.SourceRevisionId != null &&
            (p.SourceRevision!.VerificationStatus != VerificationStatus.Accepted || p.SourceRevision.RevokedAt != null ||
             p.SourceRevision.Source.ArchivedAt != null)));

    public async Task<object> ListAsync(CancellationToken ct) =>
        await Available.Where(v => !Available.Any(newer => newer.ScenarioKey == v.ScenarioKey && newer.Version > v.Version))
            .OrderBy(v => v.ScenarioKey).Select(v => new { v.ScenarioKey, v.Version, v.Title, v.PublishedAt }).ToListAsync(ct);

    public async Task<ScenarioPack> ReadAsync(string key, int version, CancellationToken ct) =>
        DiscoveryJson.Read<ScenarioPack>(await Available.Where(v => v.ScenarioKey == key && v.Version == version)
            .Select(v => v.Snapshot).SingleOrDefaultAsync(ct) ?? throw ApiException.Missing());

    public async Task PublishAsync(Guid? bvadRevision, CancellationToken ct, int? releaseVersion = null)
    {
        // Only the embedded edition is released. Free-form imports require a separate editorial review.
        var pack = Baseline();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await DatabaseLocks.AcquireAsync(db, "living-scenario-publication", ct);
        if (bvadRevision.HasValue)
        {
            var revision = await db.SourceRevisions.Include(r => r.Source).SingleOrDefaultAsync(r => r.Id == bvadRevision &&
                r.VerificationStatus == VerificationStatus.Accepted && r.RevokedAt == null && r.Source.ArchivedAt == null, ct);
            if (revision == null || (revision.Source.ExternalRecordId != "20210024855" && revision.Source.CanonicalUrl != "https://ntrs.nasa.gov/citations/20210024855") ||
                DiscoveryJson.Read<SourceMetadata>(revision.MetadataSnapshot).Title != "Life Support Baseline Values and Assumptions Document")
                throw ApiException.Gate("oxygen requires an accepted BVAD revision.");
            if (releaseVersion is < 2) throw ApiException.Invalid("A sourced edition requires version 2 or later.");
            pack = pack with { Version = releaseVersion ?? 2 };
            pack.Parameters["oxygen"] = new(.84m * 24.66m / 24m, "kg", "person/sol",
                "Modeled daily metabolic load scaled to a 24.66-hour scenario sol.", revision.Id,
                "NASA/TP-2015-218570 Rev 2, printed page 47, Total Per Day, oxygen consumption.",
                .84m, "person/24h", "https://ntrs.nasa.gov/citations/20210024855", "Modeled", "Earth");
        }
        foreach (var key in pack.Parameters.Keys.ToArray())
        {
            var p = pack.Parameters[key];
            pack.Parameters[key] = p with { OriginalValue = p.OriginalValue ?? p.Value, OriginalBasis = p.OriginalBasis ?? p.Basis };
        }
        Validate(pack);
        var snapshot = DiscoveryJson.Write(pack);
        var previous = await db.LivingScenarios.SingleOrDefaultAsync(v => v.ScenarioKey == pack.ScenarioKey && v.Version == pack.Version, ct);
        if (previous != null)
        {
            if (DiscoveryJson.Write(DiscoveryJson.Read<ScenarioPack>(previous.Snapshot)) != snapshot || previous.ArchivedAt != null) throw ApiException.Conflict("Version already released or withdrawn.");
            return;
        }
        var version = new LivingScenarioVersion { ScenarioKey = pack.ScenarioKey, Version = pack.Version,
            Title = pack.Title, Snapshot = snapshot, PublishedAt = DateTimeOffset.UtcNow };
        foreach (var (key, p) in pack.Parameters)
            version.Parameters.Add(new() { Key = key, Unit = p.Unit, OriginalValue = p.OriginalValue!.Value,
                NormalizedValue = p.Value, OriginalBasis = p.OriginalBasis!,
                NormalizedBasis = p.Basis, SourceRevisionId = p.SourceRevisionId });
        foreach (var (key, stock) in pack.Stocks)
        {
            var kind = stock.Unit == "kg" ? "Mass" : "Energy";
            var unit = db.Units.Local.FirstOrDefault(x => x.Symbol == stock.Unit && x.QuantityKind == kind)
                ?? await db.Units.SingleOrDefaultAsync(x => x.Symbol == stock.Unit && x.QuantityKind == kind, ct);
            if (unit == null) { unit = new() { Name = stock.Unit == "kg" ? "Kilogram" : "Kilowatt-hour", Symbol = stock.Unit, QuantityKind = kind }; db.Add(unit); }
            var resource = await db.Resources.SingleOrDefaultAsync(x => x.Slug == key, ct) ??
                new Resource { Name = key, Slug = key, Category = kind };
            version.Resources.Add(new() { Resource = resource, Unit = unit, InitialQuantity = stock.Initial, Capacity = stock.Capacity });
        }
        foreach (var (key, f) in pack.Facilities)
        {
            var tech = await db.Technologies.SingleOrDefaultAsync(x => x.Slug == key, ct) ?? new Technology {
                Name = key, Slug = key, Description = "Surface outpost equipment.", Capabilities = "See the published scenario rules.",
                Limitations = "Fixed sol model.", EnvironmentTested = TestEnvironment.Simulation, EvidenceType = EvidenceType.Modeled };
            version.Facilities.Add(new() { Technology = tech, DefaultCount = f.DefaultCount, MaxCount = f.MaxCount });
        }
        db.Add(version);
        foreach (var (key, definition) in pack.Facilities)
        {
            var technology = version.Facilities.Single(f => f.Technology.Slug == key).Technology;
            foreach (var flow in definition.Flows)
            {
                var resource = version.Resources.Single(r => r.Resource.Slug == flow.Resource).Resource;
                var direction = Enum.Parse<ResourceDirection>(flow.Direction);
                db.Add(new FacilityFlow { ScenarioVersionId = version.Id, TechnologyId = technology.Id,
                    ResourceId = resource.Id, ParameterKey = flow.Parameter, Phase = flow.Phase, Direction = direction });
                if (!await db.Set<TechnologyResource>().AnyAsync(x => x.TechnologyId == technology.Id &&
                    x.ResourceId == resource.Id && x.Direction == direction, ct))
                    db.Add(new TechnologyResource { Technology = technology, Resource = resource, Direction = direction });
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public static void Validate(ScenarioPack pack)
    {
        var baseline = Baseline();
        if (pack.EngineVersion != "1" || pack.LanguageCode != "en" || pack.Title != baseline.Title ||
            !pack.SolRules.SequenceEqual(baseline.SolRules)) throw ApiException.Gate("English scenario review is required.");
        if (!pack.Parameters.Keys.ToHashSet().SetEquals(baseline.Parameters.Keys) ||
            !pack.Facilities.Keys.ToHashSet().SetEquals(baseline.Facilities.Keys) ||
            !pack.Stocks.Keys.ToHashSet().SetEquals(baseline.Stocks.Keys) ||
            !pack.ActivityHoursPerPerson.SequenceEqual(baseline.ActivityHoursPerPerson) ||
            !pack.PowerPriority.SequenceEqual(baseline.PowerPriority))
            throw ApiException.Gate("Scenario definitions differ from the reviewed edition: " +
                (baseline.Parameters.Keys.Except(pack.Parameters.Keys).FirstOrDefault() ?? "configuration") + ".");
        foreach (var (key, expected) in baseline.Parameters)
        {
            if (!pack.Parameters.TryGetValue(key, out var p) || p.Unit != expected.Unit || p.Basis != expected.Basis ||
                p.Value < 0 || (p.SourceRevisionId == null && p.AssumptionNote != expected.AssumptionNote) ||
                (p.SourceRevisionId != null && string.IsNullOrWhiteSpace(p.SourceLocator)))
                throw ApiException.Gate($"Missing or invalid parameter: {key}.");
        }
        foreach (var key in new[] { "recovery", "oxygenYield", "collection", "cropThreshold" })
            if (pack.Parameters[key].Value > 1) throw ApiException.Gate($"Invalid fraction: {key}.");
        foreach (var (key, expected) in baseline.Stocks)
            if (!pack.Stocks.TryGetValue(key, out var s) || s.Unit != expected.Unit || s.Initial < 0 || s.Initial > s.Capacity)
                throw ApiException.Gate($"Invalid stock: {key}.");
        foreach (var (key, _) in baseline.Facilities)
            if (!pack.Facilities.TryGetValue(key, out var f) || f.DefaultCount < 0 || f.DefaultCount > f.MaxCount ||
                f.OperatingMode != "Continuous" || !f.Flows.SequenceEqual(baseline.Facilities[key].Flows))
                throw ApiException.Gate($"Invalid facility: {key}.");
    }
}
