using Mars_Survival_Hanbook.Data.Configurations;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data;

/// <summary>
/// Configure with UseSqlServer in the application's composition root.
/// This context does not open a connection or apply migrations on construction.
/// </summary>
public class MarsDbContext(DbContextOptions<MarsDbContext> options) : DbContext(options)
{
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleSection> ArticleSections => Set<ArticleSection>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleVersion> ArticleVersions => Set<ArticleVersion>();
    public DbSet<ScientificSource> ScientificSources => Set<ScientificSource>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<ScientificFact> ScientificFacts => Set<ScientificFact>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<ResearchPaper> ResearchPapers => Set<ResearchPaper>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Hazard> Hazards => Set<Hazard>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MarsLocation> MarsLocations => Set<MarsLocation>();
    public DbSet<ExplorationEvent> ExplorationEvents => Set<ExplorationEvent>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MapLayer> MapLayers => Set<MapLayer>();
    public DbSet<GlossaryTerm> GlossaryTerms => Set<GlossaryTerm>();
    public DbSet<GlossaryAlias> GlossaryAliases => Set<GlossaryAlias>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ReviewTask> ReviewTasks => Set<ReviewTask>();
    public DbSet<NasaConnector> NasaConnectors => Set<NasaConnector>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FeedbackReport> FeedbackReports => Set<FeedbackReport>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<ArticleSource> ArticleSources => Set<ArticleSource>();
    public DbSet<ArticleFact> ArticleFacts => Set<ArticleFact>();
    public DbSet<ArticleTechnology> ArticleTechnologies => Set<ArticleTechnology>();
    public DbSet<TechnologySource> TechnologySources => Set<TechnologySource>();
    public DbSet<TechnologyFact> TechnologyFacts => Set<TechnologyFact>();
    public DbSet<TechnologyResource> TechnologyResources => Set<TechnologyResource>();
    public DbSet<ArticleResource> ArticleResources => Set<ArticleResource>();
    public DbSet<ArticleHazard> ArticleHazards => Set<ArticleHazard>();
    public DbSet<TechnologyHazard> TechnologyHazards => Set<TechnologyHazard>();
    public DbSet<ArticleMedia> ArticleMedia => Set<ArticleMedia>();
    public DbSet<ArticleGlossaryTerm> ArticleGlossaryTerms => Set<ArticleGlossaryTerm>();
    public DbSet<ArticleRelation> ArticleRelations => Set<ArticleRelation>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        RelationshipConfiguration.Configure(modelBuilder);
        ContentConfiguration.Configure(modelBuilder);
        ScienceConfiguration.Configure(modelBuilder);
        TechnologyConfiguration.Configure(modelBuilder);
        OperationsConfiguration.Configure(modelBuilder);
        CommonConfiguration.Configure(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}

