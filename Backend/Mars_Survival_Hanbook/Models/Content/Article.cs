using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Editorial content. Publication authorization and scientific approval belong in the application layer.</summary>
public class Article : Entity
{
    [MaxLength(300)]
    public required string Title { get; set; }

    [MaxLength(200)]
    public required string Slug { get; set; }

    [MaxLength(2000)]
    public required string Summary { get; set; }

    public required string Content { get; set; }

    [MaxLength(10)]
    public required string LanguageCode { get; set; }

    public Guid? TranslationGroupId { get; set; }

    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    public ArticleType ContentType { get; set; } = ArticleType.Reference;

    [MaxLength(320)]
    public string? MetaDescription { get; set; }

    public int? EstimatedReadingMinutes { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset? NextReviewAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public Guid CategoryId { get; set; }

    public Guid CreatedById { get; set; }

    public Guid? UpdatedById { get; set; }

    public Guid? ReviewedById { get; set; }

    public Guid? PublishedById { get; set; }

    public Category Category { get; set; } = null!;

    public User CreatedBy { get; set; } = null!;

    public User? UpdatedBy { get; set; }

    public User? ReviewedBy { get; set; }

    public User? PublishedBy { get; set; }

    public ICollection<ArticleSection> Sections { get; set; } = new List<ArticleSection>();

    public ICollection<ArticleVersion> Versions { get; set; } = new List<ArticleVersion>();

    public ICollection<ReviewTask> Reviews { get; set; } = new List<ReviewTask>();

    public ICollection<FeedbackReport> FeedbackReports { get; set; } = new List<FeedbackReport>();

    public ICollection<ArticleTag> Tags { get; set; } = new List<ArticleTag>();

    public ICollection<ArticleSource> Sources { get; set; } = new List<ArticleSource>();

    public ICollection<ArticleFact> Facts { get; set; } = new List<ArticleFact>();

    public ICollection<ArticleTechnology> Technologies { get; set; } = new List<ArticleTechnology>();

    public ICollection<ArticleResource> Resources { get; set; } = new List<ArticleResource>();

    public ICollection<ArticleHazard> Hazards { get; set; } = new List<ArticleHazard>();

    public ICollection<ArticleMedia> Media { get; set; } = new List<ArticleMedia>();

    public ICollection<ArticleGlossaryTerm> GlossaryTerms { get; set; } = new List<ArticleGlossaryTerm>();

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public ICollection<ArticleRelation> RelatedArticles { get; set; } = new List<ArticleRelation>();

    public ICollection<ArticleRelation> IncomingRelations { get; set; } = new List<ArticleRelation>();
}

