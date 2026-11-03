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
        if (args.Length == 0 || args[0] is not ("seed" or "provision" or "backfill")) return false;
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarsDbContext>();
        if (args[0] == "seed") { await DiscoverySeed.RunAsync(db); Console.WriteLine("Discover Mars taxonomy, units and connectors seeded."); return true; }
        if (args[0] == "provision")
        {
            string Arg(string key) { var n = Array.IndexOf(args, key); return n >= 0 && n + 1 < args.Length ? args[n + 1] : ""; }
            var email = Arg("--email").Trim();
            if (!new EmailAddressAttribute().IsValid(email) || !Enum.TryParse<UserRole>(Arg("--role"), out var role) || !Enum.IsDefined(role))
                throw new InvalidOperationException("Usage: provision --email <email> --role Editor|ScientificReviewer|Administrator --name <name>");
            var password = Environment.GetEnvironmentVariable("MARS_BOOTSTRAP_PASSWORD");
            if (password == null || password.Length < 12) throw new InvalidOperationException("Set MARS_BOOTSTRAP_PASSWORD (at least 12 characters).");
            if (await db.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpperInvariant()))
                throw new InvalidOperationException("Account already exists; provisioning does not overwrite credentials or role.");
            var user = new User { Email = email, NormalizedEmail = email.ToUpperInvariant(), DisplayName = Arg("--name") is { Length: > 0 } name ? name : email, Role = role };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
            db.Users.Add(user); await db.SaveChangesAsync();
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
