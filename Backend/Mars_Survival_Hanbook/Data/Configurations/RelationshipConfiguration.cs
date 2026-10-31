using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class RelationshipConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Module>().ToTable("Modules");
        builder.Entity<Category>().ToTable("Categories");
        builder.Entity<Article>().ToTable("Articles");
        builder.Entity<ArticleSection>().ToTable("ArticleSections");
        builder.Entity<Tag>().ToTable("Tags");
        builder.Entity<ArticleVersion>().ToTable("ArticleVersions");
        builder.Entity<ScientificSource>().ToTable("ScientificSources");
        builder.Entity<Unit>().ToTable("Units");
        builder.Entity<ScientificFact>().ToTable("ScientificFacts");
        builder.Entity<Measurement>().ToTable("Measurements");
        builder.Entity<ResearchPaper>().ToTable("ResearchPapers");
        builder.Entity<Technology>().ToTable("Technologies");
        builder.Entity<Resource>().ToTable("Resources");
        builder.Entity<Hazard>().ToTable("Hazards");
        builder.Entity<Mission>().ToTable("Missions");
        builder.Entity<MarsLocation>().ToTable("MarsLocations");
        builder.Entity<ExplorationEvent>().ToTable("ExplorationEvents");
        builder.Entity<MediaAsset>().ToTable("MediaAssets");
        builder.Entity<MapLayer>().ToTable("MapLayers");
        builder.Entity<GlossaryTerm>().ToTable("GlossaryTerms");
        builder.Entity<GlossaryAlias>().ToTable("GlossaryAliases");
        builder.Entity<User>().ToTable("Users");
        builder.Entity<ReviewTask>().ToTable("ReviewTasks");
        builder.Entity<NasaConnector>().ToTable("NasaConnectors");
        builder.Entity<IngestionJob>().ToTable("IngestionJobs");
        builder.Entity<AuditLog>().ToTable("AuditLogs");
        builder.Entity<FeedbackReport>().ToTable("FeedbackReports");
        builder.Entity<ArticleTag>().ToTable("ArticleTags");
        builder.Entity<ArticleSource>().ToTable("ArticleSources");
        builder.Entity<ArticleFact>().ToTable("ArticleFacts");
        builder.Entity<ArticleTechnology>().ToTable("ArticleTechnologies");
        builder.Entity<TechnologySource>().ToTable("TechnologySources");
        builder.Entity<TechnologyFact>().ToTable("TechnologyFacts");
        builder.Entity<TechnologyResource>().ToTable("TechnologyResources");
        builder.Entity<ArticleResource>().ToTable("ArticleResources");
        builder.Entity<ArticleHazard>().ToTable("ArticleHazards");
        builder.Entity<TechnologyHazard>().ToTable("TechnologyHazards");
        builder.Entity<ArticleMedia>().ToTable("ArticleMedia");
        builder.Entity<ArticleGlossaryTerm>().ToTable("ArticleGlossaryTerms");
        builder.Entity<ArticleRelation>().ToTable("ArticleRelations");
        builder.Entity<Bookmark>().ToTable("Bookmarks");

        builder.Entity<ArticleTag>().HasKey(x => new { x.ArticleId, x.TagId });
        builder.Entity<ArticleSource>().HasKey(x => new { x.ArticleId, x.SourceId });
        builder.Entity<ArticleFact>().HasKey(x => new { x.ArticleId, x.FactId });
        builder.Entity<ArticleTechnology>().HasKey(x => new { x.ArticleId, x.TechnologyId });
        builder.Entity<TechnologySource>().HasKey(x => new { x.TechnologyId, x.SourceId });
        builder.Entity<TechnologyFact>().HasKey(x => new { x.TechnologyId, x.FactId });
        builder.Entity<TechnologyResource>().HasKey(x => new { x.TechnologyId, x.ResourceId, x.Direction });
        builder.Entity<ArticleResource>().HasKey(x => new { x.ArticleId, x.ResourceId });
        builder.Entity<ArticleHazard>().HasKey(x => new { x.ArticleId, x.HazardId });
        builder.Entity<TechnologyHazard>().HasKey(x => new { x.TechnologyId, x.HazardId });
        builder.Entity<ArticleMedia>().HasKey(x => new { x.ArticleId, x.MediaId });
        builder.Entity<ArticleGlossaryTerm>().HasKey(x => new { x.ArticleId, x.GlossaryTermId });
        builder.Entity<ArticleRelation>().HasKey(x => new { x.ArticleId, x.RelatedArticleId, x.RelationType });
        builder.Entity<Bookmark>().HasKey(x => new { x.UserId, x.ArticleId });

        // Include module/article identity in these relationships to reject cross-parent references.
        builder.Entity<Category>().HasAlternateKey(x => new { x.Id, x.ModuleId });
        builder.Entity<ArticleVersion>().HasAlternateKey(x => new { x.Id, x.ArticleId });

        builder.Entity<Category>().HasOne(x => x.Module)
            .WithMany(x => x.Categories).HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Category>().HasOne(x => x.Parent)
            .WithMany(x => x.Children).HasForeignKey(x => new { x.ParentId, x.ModuleId })
            .HasPrincipalKey(x => new { x.Id, x.ModuleId })
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Article>().HasOne(x => x.Category)
            .WithMany(x => x.Articles).HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Article>().HasOne(x => x.CreatedBy)
            .WithMany(x => x.AuthoredArticles).HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Article>().HasOne(x => x.UpdatedBy)
            .WithMany().HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Article>().HasOne(x => x.ReviewedBy)
            .WithMany().HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Article>().HasOne(x => x.PublishedBy)
            .WithMany().HasForeignKey(x => x.PublishedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleSection>().HasOne(x => x.Article)
            .WithMany(x => x.Sections).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleVersion>().HasOne(x => x.Article)
            .WithMany(x => x.Versions).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleVersion>().HasOne(x => x.ChangedBy)
            .WithMany().HasForeignKey(x => x.ChangedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ScientificSource>().HasOne(x => x.VerifiedBy)
            .WithMany().HasForeignKey(x => x.VerifiedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ScientificSource>().HasOne(x => x.IngestionJob)
            .WithMany(x => x.Sources).HasForeignKey(x => x.IngestionJobId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ScientificFact>().HasOne(x => x.Source)
            .WithMany(x => x.Facts).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ScientificFact>().HasOne(x => x.Unit)
            .WithMany(x => x.Facts).HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ScientificFact>().HasOne(x => x.VerifiedBy)
            .WithMany().HasForeignKey(x => x.VerifiedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Measurement>().HasOne(x => x.Fact)
            .WithMany(x => x.Measurements).HasForeignKey(x => x.FactId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Measurement>().HasOne(x => x.Source)
            .WithMany(x => x.Measurements).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Measurement>().HasOne(x => x.Unit)
            .WithMany(x => x.Measurements).HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Measurement>().HasOne(x => x.Mission)
            .WithMany(x => x.Measurements).HasForeignKey(x => x.MissionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Measurement>().HasOne(x => x.Location)
            .WithMany(x => x.Measurements).HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ResearchPaper>().HasOne(x => x.Source)
            .WithMany().HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Technology>().HasOne(x => x.VerifiedBy)
            .WithMany().HasForeignKey(x => x.VerifiedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Hazard>().HasOne(x => x.Source)
            .WithMany(x => x.Hazards).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Mission>().HasOne(x => x.Source)
            .WithMany(x => x.Missions).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<MarsLocation>().HasOne(x => x.Source)
            .WithMany(x => x.Locations).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ExplorationEvent>().HasOne(x => x.Source)
            .WithMany(x => x.ExplorationEvents).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ExplorationEvent>().HasOne(x => x.Mission)
            .WithMany(x => x.Events).HasForeignKey(x => x.MissionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<MediaAsset>().HasOne(x => x.Source)
            .WithMany(x => x.MediaAssets).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<MediaAsset>().HasOne(x => x.VerifiedBy)
            .WithMany().HasForeignKey(x => x.VerifiedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<MapLayer>().HasOne(x => x.Source)
            .WithMany(x => x.MapLayers).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<GlossaryTerm>().HasOne(x => x.Source)
            .WithMany(x => x.GlossaryTerms).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<GlossaryAlias>().HasOne(x => x.GlossaryTerm)
            .WithMany(x => x.Aliases).HasForeignKey(x => x.GlossaryTermId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ReviewTask>().HasOne(x => x.Article)
            .WithMany(x => x.Reviews).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ReviewTask>().HasOne(x => x.ArticleVersion)
            .WithMany(x => x.Reviews).HasForeignKey(x => new { x.ArticleVersionId, x.ArticleId })
            .HasPrincipalKey(x => new { x.Id, x.ArticleId })
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ReviewTask>().HasOne(x => x.SubmittedBy)
            .WithMany().HasForeignKey(x => x.SubmittedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ReviewTask>().HasOne(x => x.Reviewer)
            .WithMany(x => x.AssignedReviews).HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IngestionJob>().HasOne(x => x.Connector)
            .WithMany(x => x.Jobs).HasForeignKey(x => x.ConnectorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IngestionJob>().HasOne(x => x.RequestedBy)
            .WithMany().HasForeignKey(x => x.RequestedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<AuditLog>().HasOne(x => x.Actor)
            .WithMany().HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<AuditLog>().HasOne(x => x.IngestionJob)
            .WithMany(x => x.AuditLogs).HasForeignKey(x => x.IngestionJobId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<AuditLog>().HasOne(x => x.Source)
            .WithMany().HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FeedbackReport>().HasOne(x => x.Article)
            .WithMany(x => x.FeedbackReports).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FeedbackReport>().HasOne(x => x.ReportedBy)
            .WithMany().HasForeignKey(x => x.ReportedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FeedbackReport>().HasOne(x => x.ResolvedBy)
            .WithMany().HasForeignKey(x => x.ResolvedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleTag>().HasOne(x => x.Article)
            .WithMany(x => x.Tags).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleTag>().HasOne(x => x.Tag)
            .WithMany(x => x.Articles).HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleSource>().HasOne(x => x.Article)
            .WithMany(x => x.Sources).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleSource>().HasOne(x => x.Source)
            .WithMany(x => x.Articles).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleFact>().HasOne(x => x.Article)
            .WithMany(x => x.Facts).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleFact>().HasOne(x => x.Fact)
            .WithMany(x => x.Articles).HasForeignKey(x => x.FactId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleTechnology>().HasOne(x => x.Article)
            .WithMany(x => x.Technologies).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleTechnology>().HasOne(x => x.Technology)
            .WithMany(x => x.Articles).HasForeignKey(x => x.TechnologyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologySource>().HasOne(x => x.Technology)
            .WithMany(x => x.Sources).HasForeignKey(x => x.TechnologyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologySource>().HasOne(x => x.Source)
            .WithMany(x => x.Technologies).HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologyFact>().HasOne(x => x.Technology)
            .WithMany(x => x.Facts).HasForeignKey(x => x.TechnologyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologyFact>().HasOne(x => x.Fact)
            .WithMany(x => x.Technologies).HasForeignKey(x => x.FactId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologyResource>().HasOne(x => x.Technology)
            .WithMany(x => x.Resources).HasForeignKey(x => x.TechnologyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologyResource>().HasOne(x => x.Resource)
            .WithMany(x => x.Technologies).HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleResource>().HasOne(x => x.Article)
            .WithMany(x => x.Resources).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleResource>().HasOne(x => x.Resource)
            .WithMany(x => x.Articles).HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleHazard>().HasOne(x => x.Article)
            .WithMany(x => x.Hazards).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleHazard>().HasOne(x => x.Hazard)
            .WithMany(x => x.Articles).HasForeignKey(x => x.HazardId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologyHazard>().HasOne(x => x.Technology)
            .WithMany(x => x.Hazards).HasForeignKey(x => x.TechnologyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnologyHazard>().HasOne(x => x.Hazard)
            .WithMany(x => x.Technologies).HasForeignKey(x => x.HazardId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleMedia>().HasOne(x => x.Article)
            .WithMany(x => x.Media).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleMedia>().HasOne(x => x.Media)
            .WithMany(x => x.Articles).HasForeignKey(x => x.MediaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleGlossaryTerm>().HasOne(x => x.Article)
            .WithMany(x => x.GlossaryTerms).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleGlossaryTerm>().HasOne(x => x.GlossaryTerm)
            .WithMany(x => x.Articles).HasForeignKey(x => x.GlossaryTermId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Bookmark>().HasOne(x => x.User)
            .WithMany(x => x.Bookmarks).HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Bookmark>().HasOne(x => x.Article)
            .WithMany(x => x.Bookmarks).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleRelation>().HasOne(x => x.Article)
            .WithMany(x => x.RelatedArticles).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ArticleRelation>().HasOne(x => x.RelatedArticle)
            .WithMany(x => x.IncomingRelations).HasForeignKey(x => x.RelatedArticleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

