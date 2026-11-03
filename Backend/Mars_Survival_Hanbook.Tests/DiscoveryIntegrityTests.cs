using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Mars_Survival_Hanbook.Tests;

public sealed class DiscoveryIntegrityTests(SqlServerFixture f) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact] public async Task CompositeForeignKeysRejectCrossParentPointersAndDependencies()
    {
        Guid source2;
        Guid revision;
        await using (var db = f.CreateContext())
        {
            var s = new ScientificSource { Title = "Other", NASAOrganization = "NASA fixture", Url = "https://science.nasa.gov/mars/" };
            db.ScientificSources.Add(s);
            var r = new SourceRevision { SourceId = f.SourceId, RevisionNumber = 1, MetadataSnapshot = "{}",
                ContentHash = SourceNormalization.Hash("{}"), Fingerprint = SourceNormalization.Hash("fixture") };
            db.SourceRevisions.Add(r); await db.SaveChangesAsync();
            source2 = s.Id; revision = r.Id;
        }
        await using (var db = f.CreateContext())
        {
            var s = await db.ScientificSources.SingleAsync(x => x.Id == source2); s.CurrentAcceptedRevisionId = revision;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = f.CreateContext())
        {
            var a = f.NewArticle(); a.PublishedVersionId = f.VersionId; db.Articles.Add(a);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = f.CreateContext())
        {
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT ArticleVersionSources(ArticleVersionId,SourceId,SourceRevisionId) VALUES({f.VersionId},{source2},{revision})
                """));
        }
    }
}
