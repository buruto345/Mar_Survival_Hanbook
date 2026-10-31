using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class ContentConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Module>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Module>().HasIndex(x => x.DisplayOrder);
        builder.Entity<Category>().HasIndex(x => new { x.ModuleId, x.Slug }).IsUnique();
        builder.Entity<Category>().ToTable(t => t.HasCheckConstraint(
            "CK_Categories_NotOwnParent", "[ParentId] IS NULL OR [ParentId] <> [Id]"));

        var article = builder.Entity<Article>();
        article.HasIndex(x => x.Slug).IsUnique();
        article.HasIndex(x => new { x.CategoryId, x.Status });
        article.HasIndex(x => x.ReviewedAt);
        article.HasIndex(x => new { x.Status, x.NextReviewAt });
        article.HasIndex(x => new { x.TranslationGroupId, x.LanguageCode })
            .IsUnique().HasFilter("[TranslationGroupId] IS NOT NULL");
        article.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Articles_ReadingTime",
                "[EstimatedReadingMinutes] IS NULL OR [EstimatedReadingMinutes] > 0");
            t.HasCheckConstraint("CK_Articles_ReviewSchedule",
                "[NextReviewAt] IS NULL OR ([ReviewedAt] IS NOT NULL AND [NextReviewAt] >= [ReviewedAt])");
            t.HasCheckConstraint("CK_Articles_PublicationMetadata",
                "[Status] <> N'Published' OR ([ReviewedAt] IS NOT NULL AND [ReviewedById] IS NOT NULL " +
                "AND [PublishedAt] IS NOT NULL AND [PublishedById] IS NOT NULL)");
        });

        builder.Entity<ArticleSection>().HasIndex(x => new { x.ArticleId, x.Anchor }).IsUnique();
        builder.Entity<ArticleSection>().HasIndex(x => new { x.ArticleId, x.DisplayOrder });
        builder.Entity<Tag>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<ArticleVersion>().HasIndex(x => new { x.ArticleId, x.Version }).IsUnique();
        builder.Entity<ArticleVersion>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_ArticleVersions_PositiveVersion", "[Version] > 0");
            t.HasCheckConstraint("CK_ArticleVersions_ValidSnapshot", "ISJSON([Snapshot]) = 1");
        });
        builder.Entity<ArticleRelation>().ToTable(t => t.HasCheckConstraint(
            "CK_ArticleRelations_NoSelfLink", "[ArticleId] <> [RelatedArticleId]"));
        builder.Entity<GlossaryTerm>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<GlossaryTerm>().HasIndex(x => new { x.LanguageCode, x.Term }).IsUnique();
        builder.Entity<GlossaryAlias>().HasIndex(x => new { x.GlossaryTermId, x.Alias }).IsUnique();
        builder.Entity<GlossaryAlias>().HasIndex(x => x.Alias);
    }
}

