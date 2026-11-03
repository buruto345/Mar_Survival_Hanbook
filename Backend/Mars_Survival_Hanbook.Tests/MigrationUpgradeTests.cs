using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;
namespace Mars_Survival_Hanbook.Tests;

public sealed class MigrationUpgradeTests
{
    [SqlServerFact] public async Task UpgradePreservesLegacyContentCitationsAndReviewHistoryWithoutPublishingUnverifiedSnapshots()
    {
        var name = "MarsDiscoveryUpgradeTests_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = name,
            IntegratedSecurity = true, TrustServerCertificate = true }.ConnectionString;
        var options = new DbContextOptionsBuilder<MarsDbContext>().UseSqlServer(connection).Options;
        await using var db = new MarsDbContext(options);
        Assert.False(await db.Database.CanConnectAsync());
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261031160146_InitialCreate");
            var user = Guid.NewGuid(); var module = Guid.NewGuid(); var category = Guid.NewGuid();
            var source = Guid.NewGuid(); var article = Guid.NewGuid(); var version = Guid.NewGuid();
            var legacyJson = """{"legacy":"Original text"}""";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT Users(Id,CreatedAt,Email,NormalizedEmail,DisplayName,PasswordHash,Role,State)
                VALUES({user},SYSDATETIMEOFFSET(),'legacy@example.test','LEGACY@EXAMPLE.TEST','Legacy','placeholder','ScientificReviewer','Active');
                INSERT Modules(Id,CreatedAt,Name,Slug,DisplayOrder) VALUES({module},SYSDATETIMEOFFSET(),'Discover Mars','discover-mars',0);
                INSERT Categories(Id,CreatedAt,Name,Slug,DisplayOrder,ModuleId) VALUES({category},SYSDATETIMEOFFSET(),'Legacy','legacy',0,{module});
                INSERT ScientificSources(Id,CreatedAt,Title,NASAOrganization,Url,Authors,SourceType,VerificationStatus,RetrievedAt,VerifiedAt,VerifiedById)
                VALUES({source},SYSDATETIMEOFFSET(),'Legacy source','NASA','https://science.nasa.gov/mars/facts/','Original author','WebPage','Accepted',SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET(),{user});
                INSERT Articles(Id,CreatedAt,Title,Slug,Summary,Content,LanguageCode,Status,ContentType,CategoryId,CreatedById,ReviewedAt,ReviewedById,PublishedAt,PublishedById)
                VALUES({article},SYSDATETIMEOFFSET(),'Legacy title','legacy','Legacy summary','Original content','vi','Published','Reference',{category},{user},SYSDATETIMEOFFSET(),{user},SYSDATETIMEOFFSET(),{user});
                INSERT ArticleVersions(Id,CreatedAt,Version,Snapshot,ChangedAt,ArticleId,ChangedById)
                VALUES({version},SYSDATETIMEOFFSET(),1,{legacyJson},SYSDATETIMEOFFSET(),{article},{user});
                INSERT ArticleSources(ArticleId,SourceId,ClaimNote,EvidenceType,TestEnvironment)
                VALUES({article},{source},'Original citation','Modeled','Simulation');
                """);
            await db.GetService<IMigrator>().MigrateAsync("20261102134845_AddDiscoverMarsVersioning");
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Storage:RawRoot"] = Path.Combine(Path.GetTempPath(), name)
            }).Build();
            var services = new ServiceCollection().AddScoped(_ => new MarsDbContext(options))
                .AddSingleton(new RawStorage(config, new TestHostEnvironment())).BuildServiceProvider();
            await using (services)
            {
                await DiscoveryCommands.RunAsync(["backfill"], services);
                Assert.Empty(await db.SourceRevisions.ToListAsync());
                await DiscoveryCommands.RunAsync(["backfill", "--apply"], services);
                await db.Database.MigrateAsync();
                await DiscoveryCommands.RunAsync(["backfill", "--apply"], services);
            }
            db.ChangeTracker.Clear();
            var saved = await db.Articles.SingleAsync(x => x.Id == article);
            Assert.Equal("Original content", saved.Content);
            Assert.Equal(ArticleStatus.Draft, saved.Status); Assert.True(saved.RequiresReview); Assert.Null(saved.PublishedVersionId);
            Assert.Equal(user, saved.ReviewedById);
            Assert.Equal("""{"legacy":"Original text"}""", (await db.ArticleVersions.SingleAsync(x => x.Id == version)).Snapshot);
            Assert.Equal("Original citation", (await db.ArticleSources.SingleAsync(x => x.ArticleId == article)).ClaimNote);
            var revision = Assert.Single(await db.SourceRevisions.Where(x => x.SourceId == source).ToListAsync());
            Assert.Equal(VerificationStatus.Pending, revision.VerificationStatus); Assert.Null(revision.VerifiedById);
            Assert.Equal("legacy:" + source, (await db.ScientificSources.SingleAsync(x => x.Id == source)).IdentityKey);
            Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == article && x.Action == AuditAction.Unpublished));
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            await db.Database.MigrateAsync();
            Assert.Single(await db.SourceRevisions.Where(x => x.SourceId == source).ToListAsync());
        }
        finally
        {
            var target = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
            if (target.DataSource != @"(localdb)\MSSQLLocalDB" || target.InitialCatalog != name ||
                !Guid.TryParseExact(name["MarsDiscoveryUpgradeTests_".Length..], "N", out _))
                throw new InvalidOperationException("Unsafe temporary database cleanup target.");
            SqlConnection.ClearAllPools(); await db.Database.EnsureDeletedAsync();
        }
    }
    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
