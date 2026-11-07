using System.Text.Json.Nodes;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class DiscoveryPackImportTests(DiscoveryHttpFixture fixture) : IClassFixture<DiscoveryHttpFixture>
{
    [SqlServerFact]
    public async Task EnglishPackPreservesTextAndReimportsWithoutDuplicatesOrApprovals()
    {
        var folder = Path.Combine(Path.GetTempPath(), "DiscoveryPackTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "page.html"), "<p>Original English fixture — retained.</p>");
        var slug = "english-pack-" + Guid.NewGuid().ToString("N");
        const string original = "Original English fixture — retained.";
        var pack = new {
            Key = slug, Language = "en",
            Sources = new[] { new { Key = "page", RetrievedAt = DateTimeOffset.UtcNow,
                Metadata = new SourceMetadata("English fixture", "https://science.nasa.gov/mars/?pack=" + slug,
                    [new("Fixture", "NASA fixture")], "NASA fixture", SourceType.WebPage, Excerpt: original),
                Files = new[] { new { Path = "page.html", Name = "page", Url = "https://science.nasa.gov/mars/", MimeType = "text/html" } } } },
            Imports = Array.Empty<object>(),
            Resources = new[] { new { Key = "pressure", Kind = "facts", SourceKey = "page", UnitSymbol = "Pa",
                Data = new { Name = "Synthetic pressure", Value = 636, OriginalValue = 6.36, OriginalUnit = "mb",
                    MeasurementKind = "Estimate", Context = "Synthetic conversion test", EvidenceType = "Unspecified", TestEnvironment = "Unspecified" } } },
            Articles = new[] { new {
                Draft = new ArticleDraftRequest { Slug = slug, CategorySlug = "atmosphere-temperature-gravity", LanguageCode = "en",
                    Title = "English fixture", Summary = original, Sections = [new("overview", "Overview", original)] },
                SourceKeys = new[] { "page" }, ResourceKeys = new[] { "pressure" },
                Claims = new[] { new { Key = "c", Text = original, EvidenceType = "Unspecified", TestEnvironment = "Unspecified",
                    SourceKeys = new[] { "page" }, Locator = new { Section = "Overview" } } }
            } }
        };
        var file = Path.Combine(folder, "pack.json");
        await File.WriteAllTextAsync(file, DiscoveryJson.Write(pack));
        await DiscoveryPackImporter.RunAsync(fixture.Services, file);
        await DiscoveryPackImporter.RunAsync(fixture.Services, file);
        await using var db = fixture.Database.CreateContext();
        var article = Assert.Single(await db.Articles.Where(x => x.Slug == slug).ToListAsync());
        Assert.Equal("en", article.LanguageCode); Assert.Equal(ArticleStatus.Draft, article.Status); Assert.Null(article.PublishedVersionId);
        var version = Assert.Single(await db.ArticleVersions.Where(x => x.ArticleId == article.Id).ToListAsync());
        var snapshot = DiscoveryJson.Read<ArticleSnapshot>(version.Snapshot);
        Assert.Equal(original, snapshot.Sections[0].Body);
        Assert.Equal(636, Assert.Single(snapshot.Facts).GetProperty("value").GetDecimal());
        var sourceId = Assert.Single(snapshot.Sources).SourceId;
        var sourceRevision = Assert.Single(await db.SourceRevisions.Where(x => x.SourceId == sourceId).ToListAsync());
        Assert.Equal(VerificationStatus.Pending, sourceRevision.VerificationStatus); Assert.Null(sourceRevision.VerifiedById);
        Assert.False(await db.ReviewTasks.AnyAsync(x => x.ArticleId == article.Id));
        var importer = await db.Users.SingleAsync(x => x.Id == article.CreatedById);
        Assert.Equal(UserState.Deactivated, importer.State); Assert.Equal(UserRole.Admin, importer.Role);

        var edited = JsonNode.Parse(await File.ReadAllTextAsync(file))!;
        const string revised = "Updated English fixture; previous evidence remains available.";
        edited["articles"]![0]!["draft"]!["sections"]![0]!["body"] = revised;
        await File.WriteAllTextAsync(file, edited.ToJsonString());
        await DiscoveryPackImporter.RunAsync(fixture.Services, file, refreshDrafts: true);
        await db.Entry(article).ReloadAsync();
        var versions = await db.ArticleVersions.AsNoTracking().Where(x => x.ArticleId == article.Id).OrderBy(x => x.Version).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal(version.SnapshotHash, versions[0].SnapshotHash);
        Assert.Equal(original, DiscoveryJson.Read<ArticleSnapshot>(versions[0].Snapshot).Sections[0].Body);
        Assert.Equal(revised, DiscoveryJson.Read<ArticleSnapshot>(versions[1].Snapshot).Sections[0].Body);
        Assert.Equal(versions[1].Id, article.CurrentDraftVersionId);
        Assert.Null(article.PublishedVersionId);
    }
}
