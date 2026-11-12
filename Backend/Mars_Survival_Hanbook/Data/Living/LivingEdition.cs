using System.Security.Claims;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Living;

internal static class LivingEdition
{
    private const string Report = "https://ntrs.nasa.gov/citations/20210024855";
    private static readonly (string Category, string Title, string Text)[] Guides = [
        ("housing", "Making room for the crew", "Each active habitat adds sleeping places and a base electricity load. To expand, reserve building materials and assign construction hours. The new places become available the sol after completion."),
        ("water-and-oxygen", "Choosing which machine runs first", "The recycler draws from the opening wastewater tank. The electrolyzer draws from clean water. Give either machine first access to available electricity and compare its output with the other machine's output."),
        ("food", "Planning for the first harvest", "Rations cover the interval between planting and harvest. A crop batch needs water, electricity and cultivation hours. Harvest enters the food store after meals on the last growth sol."),
        ("energy", "Reading the daily electricity balance", "Solar arrays supply the habitat load first. Machines share the remaining generation and available battery discharge in priority order. Surplus generation can recharge the battery."),
        ("sanitation-and-waste", "Following used water into storage", "Drinking and washing draw from the clean tank. Only water actually supplied contributes to collection. New wastewater enters storage after the recycler has finished, and any overflow leaves the outpost."),
        ("community", "Allocating crew time", "Assign hours to sleep, hygiene, maintenance, cultivation, construction and community life. When the crew has assigned all available hours, increasing one activity requires reducing another.")
    ];

    public static async Task<Guid> PublishAsync(IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<MarsDbContext>();
        var identity = await ReleaseIdentityAsync(db, ct);
        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, identity.Id.ToString()), new Claim(ClaimTypes.Role, "Admin")
        ], "LocalContentRelease")) };
        try
        {
            var module = new ContentModule(ContentModule.Living);
            var articles = ActivatorUtilities.CreateInstance<ArticleService>(sp, module);
            var reviews = ActivatorUtilities.CreateInstance<ReviewService>(sp, module);
            const string title = "Life Support Baseline Values and Assumptions Document";
            const string note = "The daily metabolic load table on printed page 47 gives an oxygen demand of 0.84 kg over 24 hours.";
            var sources = sp.GetRequiredService<SourceService>();
            var (revision, _) = await sources.ManualAsync(new(Report, title, [
                new("Michael K. Ewert", "Johnson Space Center"), new("Thomas T. Chen", "Jacobs (United States)"),
                new("Camilah D. Powell", "Jacobs (United States)")], "NASA Johnson Space Center", SourceType.TechnicalReport,
                note, "The report underlies the metabolic reference. Outpost equipment and activity settings are teaching assumptions.",
                new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero)), ct);
            if (revision.VerificationStatus == VerificationStatus.Pending && revision.RevokedAt == null)
                await sources.DecideAsync(revision.Id, new(VerificationStatus.Accepted, note), Concurrency.ETag(revision), ct);
            if (revision.VerificationStatus != VerificationStatus.Accepted || revision.RevokedAt != null)
                throw ApiException.Gate("The reference edition has been withdrawn.");
            foreach (var (category, heading, body) in Guides)
            {
                var slug = "outpost-" + category;
                var article = await db.Articles.SingleOrDefaultAsync(a => a.Slug == slug, ct);
                if (article != null && article.CreatedById != identity.Id) throw ApiException.Conflict("A guide slug is already in use.");
                if (article?.PublishedAt != null) continue;
                article ??= await articles.SaveDraftAsync(null, new() { Slug = slug, LanguageCode = "en", CategorySlug = category,
                    Title = heading, Summary = body, Sections = [new("scenario", "In this scenario", body)],
                    EnglishCitations = new() { [revision.Id] = new(title, null,
                        "The report underlies the metabolic reference. Outpost equipment and activity settings are teaching assumptions.") },
                    Claims = [new("baseline", "The BVAD supplies reference values for comparing life support studies.",
                        EvidenceType.Modeled, TestEnvironment.Simulation, [revision.Id], new() { ["section"] = "Abstract" })] }, null, ct);
                var version = await articles.VersionAsync(article.Id, article.CurrentDraftVersionId!.Value, ct);
                var frozen = SnapshotService.Read(version);
                if (version.ChangedById != identity.Id || frozen.Title != heading || frozen.Summary != body ||
                    frozen.Sections.Single().Body != body || frozen.Sources.Single().SourceRevisionId != revision.Id)
                    throw ApiException.Gate("A guide has entered a different editorial workflow.");
                if (!await db.LivingEnglishReviews.AnyAsync(r => r.ArticleVersionId == version.Id, ct))
                {
                    db.LivingEnglishReviews.Add(new() { ArticleVersion = version, SnapshotHash = version.SnapshotHash,
                        ReviewerId = identity.Id, ReviewedAt = DateTimeOffset.UtcNow });
                    await db.SaveChangesAsync(ct);
                }
                var task = version.ReviewState == ArticleVersionReviewState.Draft
                    ? await articles.SubmitAsync(article.Id, version.Id, Concurrency.ETag(article), ct)
                    : await db.ReviewTasks.SingleAsync(t => t.ArticleVersionId == version.Id, ct);
                if (task.State == ReviewState.Pending) await reviews.ClaimAsync(task.Id, Concurrency.ETag(task), ct);
                if (task.State == ReviewState.InProgress)
                    await reviews.DecideAsync(task.Id, new(ReviewState.Approved, "English guide and citation checked.",
                        new(true, true, true, true, true)), Concurrency.ETag(task), ct);
                await articles.PublishAsync(article.Id, version.Id, Concurrency.ETag(article), ct);
            }
            return revision.Id;
        }
        finally { accessor.HttpContext = previous; }
    }

    private static async Task<User> ReleaseIdentityAsync(MarsDbContext db, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "living-release-identity", ct);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUsername == "LIVING-RELEASE", ct);
        if (user == null)
        {
            user = new() { Username = "living-release", NormalizedUsername = "LIVING-RELEASE", Role = UserRole.Admin,
                State = UserState.Deactivated, PasswordHash = "!disabled-living-release", FullName = "Living on Mars local release" };
            db.Add(user);
            await db.SaveChangesAsync(ct);
        }
        if (user.State != UserState.Deactivated || user.PasswordHash != "!disabled-living-release" || user.Role != UserRole.Admin)
            throw ApiException.Conflict("Reserved release identity has changed.");
        await tx.CommitAsync(ct);
        return user;
    }
}
