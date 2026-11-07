using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars_Survival_Hanbook.BackgroundJobs;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Ingestion;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data;

/// <summary>Local operator command. Uses normal domain services; never approves or publishes imported material.</summary>
public static class DiscoveryPackImporter
{
    public static async Task RunAsync(IServiceProvider services, string file, bool retryFailed = false, bool refreshDrafts = false)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(file))!;
        var pack = JsonNode.Parse(await File.ReadAllTextAsync(file))!.AsObject();
        var key = pack["key"]!.GetValue<string>();
        if (key.Length > 100 || pack["language"]?.GetValue<string>() != "en") throw new InvalidOperationException("Expected an English pack with a bounded key.");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarsDbContext>();
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply migrations before importing data.");
        await DiscoverySeed.RunAsync(db);
        var actor = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == "NASA-IMPORT@LOCAL.INVALID");
        if (actor == null)
        {
            actor = new User { Email = "nasa-import@local.invalid", NormalizedEmail = "NASA-IMPORT@LOCAL.INVALID",
                Username = "nasa-import", NormalizedUsername = "NASA-IMPORT",
                FullName = "NASA English data import (system)", Role = UserRole.Admin, State = UserState.Deactivated,
                PasswordHash = "!disabled-no-interactive-login" };
            db.Users.Add(actor); await db.SaveChangesAsync();
        }
        if (actor.State != UserState.Deactivated || actor.Role != UserRole.Admin)
            throw new InvalidOperationException("Reserved import identity must remain a deactivated Admin.");
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var previousContext = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString()), new Claim(ClaimTypes.Role, nameof(UserRole.Admin))], "LocalOperatorCommand")) };
        try
        {
            var sources = scope.ServiceProvider.GetRequiredService<SourceService>();
            var raw = scope.ServiceProvider.GetRequiredService<RawStorage>();
            var content = scope.ServiceProvider.GetRequiredService<ContentService>();
            var articles = scope.ServiceProvider.GetRequiredService<ArticleService>();
            var revisions = new Dictionary<string, Guid>();
            var resources = new Dictionary<string, (Guid Id, string Kind)>();
            var report = new List<object>();
            foreach (var entry in pack["sources"]!.AsArray().OfType<JsonObject>())
            {
                var sourceKey = entry["key"]!.GetValue<string>();
                var blobs = new List<object>();
                foreach (var item in entry["files"]!.AsArray().OfType<JsonObject>())
                {
                    var path = Path.GetFullPath(Path.Combine(directory, item["path"]!.GetValue<string>()));
                    if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Evidence files must be inside the data pack directory.");
                    var stored = await raw.PutAsync(await File.ReadAllBytesAsync(path), default);
                    blobs.Add(new { Name = item["name"]!.GetValue<string>(), Url = item["url"]!.GetValue<string>(),
                        MimeType = item["mimeType"]!.GetValue<string>(), Blob = stored.Path, Sha256 = stored.Hash });
                }
                var manifest = await raw.PutAsync(Encoding.UTF8.GetBytes(DiscoveryJson.Write(new { Files = blobs })), default);
                var metadata = entry["metadata"]!.Deserialize<SourceMetadata>(DiscoveryJson.Options)!;
                var result = await sources.StoreAsync(new(SourceProvider.NasaWeb, metadata.Url, null, metadata,
                    manifest.Path, manifest.Hash, entry["retrievedAt"]!.GetValue<DateTimeOffset>()), null, actor.Id, default);
                revisions[sourceKey] = result.Revision.Id;
                report.Add(new { Kind = "source", Key = sourceKey, result.Revision.Id, result.Created });
                Console.WriteLine($"Source {sourceKey}: {(result.Created ? "created" : "reused")}");
            }
            foreach (var entry in pack["imports"]?.AsArray().OfType<JsonObject>() ?? [])
            {
                var request = entry["request"]!.Deserialize<ImportRequest>(DiscoveryJson.Options)!;
                var import = scope.ServiceProvider.GetRequiredService<ImportService>();
                var job = await import.CreateAsync(request, key + ":" + entry["key"]!.GetValue<string>(), default);
                if (retryFailed && job.Status is IngestionStatus.Failed or IngestionStatus.PartiallySucceeded)
                    job = await import.RetryAsync(job.Id, Concurrency.ETag(job), default);
                // Use a fresh scope per worker lease, exactly as the hosted worker does.
                for (var attempt = 0; attempt < 30 && job.Status is IngestionStatus.Queued or IngestionStatus.Running; attempt++)
                {
                    using var workerScope = services.CreateScope();
                    await workerScope.ServiceProvider.GetRequiredService<IngestionProcessor>().RunOneAsync(default);
                    await db.Entry(job).ReloadAsync();
                    if (job.Status is IngestionStatus.Queued or IngestionStatus.Running)
                        await Task.Delay(TimeSpan.FromSeconds(2));
                }
                report.Add(new { Kind = "job", Key = entry["key"]!.GetValue<string>(), job.Id, job.Status, job.ErrorMessage });
                Console.WriteLine($"Import {entry["key"]}: {job.Status}");
            }
            foreach (var source in await db.ScientificSources.Where(x => x.Provider != SourceProvider.NasaWeb).ToListAsync())
            {
                var revision = await db.SourceRevisions.Where(x => x.SourceId == source.Id).OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync();
                if (revision != null) revisions[source.Provider.ToString().ToLowerInvariant() + ":" + source.ExternalRecordId] = revision.Id;
            }
            foreach (var media in await db.MediaAssets.Where(x => x.NasaId != null).ToListAsync())
                resources["image:" + media.NasaId] = (media.Id, "media");
            foreach (var layer in await db.MapLayers.ToListAsync()) resources["layer:" + layer.LayerIdentifier] = (layer.Id, "map-layers");
            foreach (var entry in pack["resources"]!.AsArray().OfType<JsonObject>())
            {
                var resourceKey = entry["key"]!.GetValue<string>();
                var kind = entry["kind"]!.GetValue<string>();
                var marker = key + ":" + resourceKey;
                await using var tx = await db.Database.BeginTransactionAsync();
                await DatabaseLocks.AcquireAsync(db, "data-pack:" + marker, default);
                var existing = await db.AuditLogs.Where(x => x.EntityType == "DiscoveryDataPackResource" && x.ChangeSummary == marker)
                    .Select(x => (Guid?)x.EntityId).SingleOrDefaultAsync();
                if (existing.HasValue) { resources[resourceKey] = (existing.Value, kind); await tx.CommitAsync(); continue; }
                var data = entry["data"]!.DeepClone().AsObject();
                data["sourceRevisionId"] = revisions[entry["sourceKey"]!.GetValue<string>()];
                if (entry["unitSymbol"] is { } symbol)
                    data["unitId"] = await db.Units.Where(x => x.Symbol == symbol.GetValue<string>()).Select(x => x.Id).SingleAsync();
                foreach (var relation in new[] { "mission", "location", "fact" })
                    if (entry[relation + "Key"] is { } reference) data[relation + "Id"] = resources[reference.GetValue<string>()].Id;
                ProvenancedRequest request = kind switch {
                    "facts" => data.Deserialize<FactRequest>(DiscoveryJson.Options)!,
                    "missions" => data.Deserialize<MissionRequest>(DiscoveryJson.Options)!,
                    "locations" => data.Deserialize<LocationRequest>(DiscoveryJson.Options)!,
                    "timeline-events" => data.Deserialize<TimelineEventRequest>(DiscoveryJson.Options)!,
                    "measurements" => data.Deserialize<MeasurementRequest>(DiscoveryJson.Options)!,
                    _ => throw new InvalidOperationException("Unsupported pack resource kind: " + kind)
                };
                var result = await content.SaveAsync(kind, null, request, null, default);
                Audit.Add(db, actor.Id, "DiscoveryDataPackResource", result.Entity.Id, AuditAction.Created, marker);
                await db.SaveChangesAsync(); await tx.CommitAsync();
                resources[resourceKey] = (result.Entity.Id, kind);
            }
            foreach (var entry in pack["articles"]!.AsArray().OfType<JsonObject>())
            {
                var draft = entry["draft"]!.Deserialize<ArticleDraftRequest>(DiscoveryJson.Options)!;
                if (draft.LanguageCode != "en") throw new InvalidOperationException("Article language must remain English.");
                var existing = await db.Articles.SingleOrDefaultAsync(x => x.Slug == draft.Slug);
                if (existing != null)
                {
                    if (existing.CreatedById != actor.Id || existing.LanguageCode != "en") throw new InvalidOperationException("Slug belongs to different content: " + draft.Slug);
                    if (!refreshDrafts) { report.Add(new { Kind = "article", existing.Id, existing.Slug, Created = false }); continue; }
                    if (existing.Status != ArticleStatus.Draft || existing.PublishedVersionId != null ||
                        await db.ArticleVersions.AnyAsync(x => x.ArticleId == existing.Id && x.ChangedById != actor.Id) ||
                        await db.ReviewTasks.AnyAsync(x => x.ArticleId == existing.Id))
                        throw new InvalidOperationException("Cannot refresh content that has entered a human editorial workflow: " + draft.Slug);
                }
                draft.SourceRevisionIds = entry["sourceKeys"]!.AsArray().Select(x => revisions[x!.GetValue<string>()]).Distinct().ToArray();
                draft.Claims = entry["claims"]!.AsArray().OfType<JsonObject>().Select(c => new ClaimInput(
                    c["key"]!.GetValue<string>(), c["text"]!.GetValue<string>(), Enum.Parse<EvidenceType>(c["evidenceType"]!.GetValue<string>()),
                    Enum.Parse<TestEnvironment>(c["testEnvironment"]!.GetValue<string>()),
                    c["sourceKeys"]!.AsArray().Select(x => revisions[x!.GetValue<string>()]).ToArray(),
                    c["locator"]!.Deserialize<Dictionary<string, string>>())).ToArray();
                var requestedKeys = entry["resourceKeys"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();
                var missingKeys = requestedKeys.Where(x => !resources.ContainsKey(x)).ToArray();
                if (missingKeys.Length > 0)
                {
                    report.Add(new { Kind = "incomplete-article", draft.Slug, MissingResourceKeys = missingKeys });
                    Console.WriteLine("Draft deferred (missing resources): " + draft.Slug); continue;
                }
                var selected = requestedKeys.Select(x => resources[x]).ToArray();
                Guid[] Of(string kind) => selected.Where(x => x.Kind == kind).Select(x => x.Id).Distinct().ToArray();
                draft.ResourceRefs = new() { FactIds = Of("facts"), MissionIds = Of("missions"), LocationIds = Of("locations"),
                    TimelineEventIds = Of("timeline-events"), MeasurementIds = Of("measurements"), MediaIds = Of("media"), MapLayerIds = Of("map-layers") };
                var article = await articles.SaveDraftAsync(existing?.Id, draft, existing == null ? null : Concurrency.ETag(existing), default);
                report.Add(new { Kind = "article", article.Id, article.Slug, Created = existing == null, article.CurrentDraftVersionId });
                Console.WriteLine("Draft saved: " + article.Slug);
            }
            var output = new { Pack = key, Language = "en", ImportedAt = DateTimeOffset.UtcNow, ActorId = actor.Id,
                Revisions = revisions, Resources = resources.ToDictionary(x => x.Key, x => new { x.Value.Id, x.Value.Kind }), Results = report };
            await File.WriteAllTextAsync(Path.Combine(directory, "import-report.json"), JsonSerializer.Serialize(output, new JsonSerializerOptions(DiscoveryJson.Options) { WriteIndented = true }));
            Console.WriteLine("Import report: " + Path.Combine(directory, "import-report.json"));
        }
        finally { accessor.HttpContext = previousContext; }
    }
}
