using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Mars_Survival_Hanbook.Data;

public static class DiscoveryCommands
{
    public static async Task<bool> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.FirstOrDefault() == "export-living-review")
        {
            if (args.Length != 3 || !Guid.TryParse(args[1], out var id))
                throw ApiException.Invalid("Specify the English article version ID and a new local output file.");
            using var exportScope = services.CreateScope();
            var dbForExport = exportScope.ServiceProvider.GetRequiredService<MarsDbContext>();
            var version = await dbForExport.ArticleVersions.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id &&
                v.Article.Category.Module.Slug == ContentModule.Living && v.LanguageCode == "en") ?? throw ApiException.Missing();
            await using var file = new FileStream(args[2], FileMode.CreateNew, FileAccess.Write);
            await file.WriteAsync(System.Text.Encoding.UTF8.GetBytes(version.Snapshot));
            Console.WriteLine(version.SnapshotHash);
            return true;
        }
        if (args.FirstOrDefault() is "publish-living-scenario" or "withdraw-living-scenario")
        {
            using var livingScope = services.CreateScope();
            var livingDb = livingScope.ServiceProvider.GetRequiredService<MarsDbContext>();
            if (args[0] == "withdraw-living-scenario")
            {
                if (args.Length != 3 || !int.TryParse(args[2], out var version)) throw ApiException.Invalid("Specify the scenario key and version.");
                var row = await livingDb.LivingScenarios.SingleOrDefaultAsync(v => v.ScenarioKey == args[1] && v.Version == version)
                    ?? throw ApiException.Missing();
                row.ArchivedAt ??= DateTimeOffset.UtcNow;
                await livingDb.SaveChangesAsync();
            }
            else
            {
                Guid? revision = null;
                int edition = 2;
                if (args.Length is 3 or 5 && args[1] == "--bvad-revision" && Guid.TryParse(args[2], out var id)) revision = id;
                else if (args.Length != 1) throw ApiException.Invalid("Use publish-living-scenario [--bvad-revision ID [--version N]].");
                if (args.Length == 5 && (args[3] != "--version" || !int.TryParse(args[4], out edition) || edition < 2))
                    throw ApiException.Invalid("Specify an integer version of at least 2.");
                using var lockScope = services.CreateScope();
                var lockDb = lockScope.ServiceProvider.GetRequiredService<MarsDbContext>();
                // A second local release waits for the whole edition, including the six CMS reviews.
                lockDb.Database.SetCommandTimeout(0);
                await using var releaseLock = await lockDb.Database.BeginTransactionAsync();
                await DatabaseLocks.AcquireAsync(lockDb, "living-release-command", default, timeoutMilliseconds: -1);
                await DiscoverySeed.RunAsync(livingDb);
                var service = livingScope.ServiceProvider.GetRequiredService<Services.Living.LivingScenarioService>();
                if (revision == null)
                {
                    await service.PublishAsync(null, default);
                    revision = await Living.LivingEdition.PublishAsync(livingScope.ServiceProvider, default);
                }
                await service.PublishAsync(revision, default, edition);
                await releaseLock.CommitAsync();
                Console.WriteLine($"Published outpost version {edition}.");
            }
            return true;
        }
        if (args.FirstOrDefault() == "publish-survival-edition")
        {
            await Survival.SurvivalRelease.RunAsync(services);
            return true;
        }
        if (args.Length > 0 && args[0] == "import-discovery-pack")
        {
            var index = Array.IndexOf(args, "--file");
            if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Usage: import-discovery-pack --file <local-pack.json>");
            await DiscoveryPackImporter.RunAsync(services, args[index + 1], args.Contains("--retry-failed"), args.Contains("--refresh-drafts"));
            return true;
        }
        if (args.Length == 0 || args[0] is not ("seed" or "provision" or "backfill")) return false;
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarsDbContext>();
        if (args[0] == "seed") { await DiscoverySeed.RunAsync(db); Console.WriteLine("Discover Mars taxonomy, units and connectors seeded."); return true; }
        if (args[0] == "provision")
        {
            string Arg(string key) { var n = Array.IndexOf(args, key); return n >= 0 && n + 1 < args.Length ? args[n + 1] : ""; }
            var email = Arg("--email").Trim();
            if (Arg("--role") is not ("Admin" or "Viewer") ||
                !Enum.TryParse<UserRole>(Arg("--role"), out var role))
                throw new InvalidOperationException("Usage: provision --username <username> --role Admin|Viewer [--full-name <full name>] [--email <email>] (name/email required for Viewer)");
            var password = Environment.GetEnvironmentVariable("MARS_BOOTSTRAP_PASSWORD");
            if (string.IsNullOrWhiteSpace(password) || (role != UserRole.Admin && password.Length < 12))
                throw new InvalidOperationException("Set MARS_BOOTSTRAP_PASSWORD (at least 12 characters for Viewer).");
            var user = await scope.ServiceProvider.GetRequiredService<AuthenticationService>().ProvisionAsync(new RegisterRequest {
                Username = Arg("--username"), FullName = Arg("--full-name"), Email = email, Password = password
            }, role, default);
            Console.WriteLine("Provisioned account " + user.Id); return true;
        }
        var apply = args.Contains("--apply");
        var sources = await db.ScientificSources.ToListAsync();
        var missing = new List<Guid>();
        var candidates = new Dictionary<string, List<Guid>>();
        foreach (var s in sources)
        {
            var key = SourceNormalization.CanonicalUrl(s.CanonicalUrl ?? s.Url);
            if (!candidates.TryGetValue(key, out var ids)) candidates[key] = ids = [];
            ids.Add(s.Id);
            if (!await db.SourceRevisions.AnyAsync(r => r.SourceId == s.Id)) missing.Add(s.Id);
        }
        Console.WriteLine(DiscoveryJson.Write(new { DryRun = !apply, SourceCount = sources.Count, SourcesWithoutRevision = missing,
            PotentialDuplicateUrls = candidates.Where(x => x.Value.Count > 1).Select(x => new { Url = x.Key, SourceIds = x.Value }).ToArray(),
            LegacyVersions = await db.ArticleVersions.CountAsync(x => x.SnapshotHash == ""),
            PublicationsNeedingReview = await db.Articles.CountAsync(x => x.RequiresReview && x.PublishedVersionId == null) }));
        if (apply)
        {
            var raw = scope.ServiceProvider.GetRequiredService<RawStorage>();
            await using var tx = await db.Database.BeginTransactionAsync();
            await DatabaseLocks.AcquireAsync(db, "legacy-backfill", default);
            foreach (var s in sources.Where(s => missing.Contains(s.Id)))
            {
                if (await db.SourceRevisions.AnyAsync(x => x.SourceId == s.Id)) continue;
                var m = new SourceMetadata(s.Title, s.Url, string.IsNullOrWhiteSpace(s.Authors) ? [] : [new(s.Authors, "")],
                    s.NASAOrganization, s.SourceType,
                    Excerpt: s.VerificationNotes, UsageNote: "Legacy content requires review; historical source row retained.", ExternalRecordId: s.ExternalRecordId,
                    ProviderMetadata: System.Text.Json.JsonSerializer.SerializeToElement(new {
                        s.Authors, s.NASAOrganization, s.VerificationStatus, s.VerifiedAt, s.VerifiedById, s.PublishedAt, s.Doi, s.Checksum
                    }, DiscoveryJson.Options));
                var payload = DiscoveryJson.Write(m);
                var stored = await raw.PutAsync(System.Text.Encoding.UTF8.GetBytes(payload), default);
                var hash = SourceNormalization.Fingerprint(m, null);
                db.SourceRevisions.Add(new() { SourceId = s.Id, RevisionNumber = 1, MetadataSnapshot = payload,
                    ContentHash = hash.ContentHash, Fingerprint = hash.Fingerprint, RawStoragePath = stored.Path,
                    RawChecksum = stored.Hash, RetrievedAt = s.RetrievedAt, VerificationStatus = VerificationStatus.Pending });
                Audit.Add(db, null, nameof(ScientificSource), s.Id, AuditAction.Updated, "Legacy source preserved as Pending; no historical review inferred.");
            }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        return true;
    }
}
