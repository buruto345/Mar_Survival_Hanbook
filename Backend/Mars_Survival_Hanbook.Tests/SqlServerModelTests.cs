using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public class SqlServerModelTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task InitialMigrationIsAppliedAndUpdatingAgainPreservesData()
    {
        await using var context = fixture.CreateContext();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Single(applied);
        Assert.EndsWith("_InitialCreate", applied[0]);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());

        await context.Database.MigrateAsync();

        Assert.True(await context.Articles.AnyAsync(x => x.Id == fixture.ArticleId));
        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());
    }

    [SqlServerFact]
    public async Task ArticleGraphAndScientificValuesRoundTripWithoutLosingProvenance()
    {
        Guid articleId;
        Guid factId;
        await using (var context = fixture.CreateContext())
        {
            var article = fixture.NewArticle();
            var fact = fixture.NewFact();
            article.Facts.Add(new ArticleFact { Fact = fact, DisplayOrder = 1 });
            article.Sections.Add(new ArticleSection { Heading = "Overview", Anchor = "overview", Body = "Test" });
            context.Articles.Add(article);
            await context.SaveChangesAsync();
            articleId = article.Id;
            factId = fact.Id;
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.Articles.Include(x => x.Sections)
            .Include(x => x.Facts).ThenInclude(x => x.Fact).ThenInclude(x => x.Source)
            .SingleAsync(x => x.Id == articleId);
        var savedFact = Assert.Single(saved.Facts).Fact;
        Assert.Equal(factId, savedFact.Id);
        Assert.Equal(12.123456789012m, savedFact.Value);
        Assert.Equal(0.012123456789m, savedFact.OriginalValue);
        Assert.Equal("kg/h", savedFact.OriginalUnit);
        Assert.Equal(fixture.SourceId, savedFact.Source.Id);
        Assert.Equal(ArticleStatus.Draft, saved.Status);
        Assert.Equal("overview", Assert.Single(saved.Sections).Anchor);
        Assert.NotEmpty(saved.RowVersion);
    }

    [SqlServerFact]
    public async Task DuplicateArticleSlugIsRejected()
    {
        await using var context = fixture.CreateContext();
        context.Articles.Add(fixture.NewArticle("test-article"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task CategoryCannotReferenceParentFromAnotherModule()
    {
        await using var context = fixture.CreateContext();
        var module = new Module { Name = "Other module", Slug = Guid.NewGuid().ToString("N") };
        context.Modules.Add(module);
        await context.SaveChangesAsync();
        context.Categories.Add(new Category
        {
            Name = "Invalid child", Slug = "invalid-child", ModuleId = module.Id, ParentId = fixture.CategoryId
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task ReviewCannotReferenceVersionOfAnotherArticle()
    {
        await using var context = fixture.CreateContext();
        var article = fixture.NewArticle();
        context.Articles.Add(article);
        await context.SaveChangesAsync();
        // Do not load the version navigation: prove the SQL FK rejects mismatched identifiers.
        context.ReviewTasks.Add(new ReviewTask
        {
            ArticleId = article.Id, ArticleVersionId = fixture.VersionId, SubmittedById = fixture.UserId
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task ApprovalRequiresCompletedScientificChecklist()
    {
        await using var context = fixture.CreateContext();
        context.ReviewTasks.Add(new ReviewTask
        {
            ArticleId = fixture.ArticleId, ArticleVersionId = fixture.VersionId,
            SubmittedById = fixture.UserId, ReviewerId = fixture.UserId,
            State = ReviewState.Approved, CompletedAt = DateTimeOffset.UtcNow.AddMinutes(1)
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task CompletedReviewCanBeStoredForItsExactVersion()
    {
        await using var context = fixture.CreateContext();
        var review = new ReviewTask
        {
            ArticleId = fixture.ArticleId, ArticleVersionId = fixture.VersionId,
            SubmittedById = fixture.UserId, ReviewerId = fixture.UserId,
            State = ReviewState.Approved, CompletedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            SourcesChecked = true, ClaimsChecked = true, UnitsChecked = true,
            EvidenceChecked = true, MediaRightsChecked = true
        };
        context.ReviewTasks.Add(review);
        await context.SaveChangesAsync();
        Assert.NotEmpty(review.RowVersion);
    }

    [SqlServerFact]
    public async Task PublishingWithoutReviewMetadataIsRejected()
    {
        await using var context = fixture.CreateContext();
        var article = fixture.NewArticle();
        article.Status = ArticleStatus.Published;
        context.Articles.Add(article);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task ReferencedSourceCannotBeHardDeleted()
    {
        await using var context = fixture.CreateContext();
        var source = await context.ScientificSources.SingleAsync(x => x.Id == fixture.SourceId);
        context.ScientificSources.Remove(source);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task RowVersionRejectsStaleUpdatesAndTimestampsAreMaintained()
    {
        Guid articleId;
        await using (var setup = fixture.CreateContext())
        {
            var article = fixture.NewArticle();
            setup.Articles.Add(article);
            await setup.SaveChangesAsync();
            articleId = article.Id;
        }

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var a = await first.Articles.SingleAsync(x => x.Id == articleId);
        var b = await second.Articles.SingleAsync(x => x.Id == articleId);
        var createdAt = a.CreatedAt;
        a.Title = "First editor";
        a.CreatedAt = createdAt.AddDays(-1);
        await first.SaveChangesAsync();
        b.Title = "Second editor";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verify = fixture.CreateContext();
        var saved = await verify.Articles.SingleAsync(x => x.Id == articleId);
        Assert.Equal("First editor", saved.Title);
        Assert.Equal(createdAt, saved.CreatedAt);
        Assert.NotNull(saved.UpdatedAt);
    }

    [SqlServerFact]
    public async Task DuplicateBookmarkIsRejectedByCompositeKey()
    {
        await using (var context = fixture.CreateContext())
        {
            context.Bookmarks.Add(new Bookmark { UserId = fixture.UserId, ArticleId = fixture.ArticleId });
            await context.SaveChangesAsync();
        }
        await using var second = fixture.CreateContext();
        second.Bookmarks.Add(new Bookmark { UserId = fixture.UserId, ArticleId = fixture.ArticleId });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task AResourceCanBeBothInputAndOutputOfOneTechnology()
    {
        await using var context = fixture.CreateContext();
        var technology = new Technology
        {
            Name = "Test cycle", Slug = Guid.NewGuid().ToString("N"), Description = "Synthetic test",
            Capabilities = "Test", Limitations = "Test only"
        };
        var resource = new Resource
        {
            Name = "Test resource", Slug = Guid.NewGuid().ToString("N"), Category = "Test"
        };
        technology.Resources.Add(new TechnologyResource { Resource = resource, Direction = ResourceDirection.Input });
        technology.Resources.Add(new TechnologyResource { Resource = resource, Direction = ResourceDirection.Output });
        context.Technologies.Add(technology);
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.TechnologyResources.CountAsync(x => x.TechnologyId == technology.Id));
    }

    [SqlServerFact]
    public async Task UndefinedEnumValueIsRejected()
    {
        await using var context = fixture.CreateContext();
        var article = fixture.NewArticle();
        article.Status = (ArticleStatus)999;
        context.Articles.Add(article);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task ArticleCannotLinkToItself()
    {
        await using var context = fixture.CreateContext();
        context.ArticleRelations.Add(new ArticleRelation
        {
            ArticleId = fixture.ArticleId, RelatedArticleId = fixture.ArticleId
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task VerifiedFactRequiresExplicitEvidenceAndTestEnvironment()
    {
        await using var context = fixture.CreateContext();
        var fact = fixture.NewFact();
        fact.VerificationStatus = VerificationStatus.Accepted;
        fact.VerifiedAt = DateTimeOffset.UtcNow;
        fact.VerifiedById = fixture.UserId;
        context.ScientificFacts.Add(fact);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}

