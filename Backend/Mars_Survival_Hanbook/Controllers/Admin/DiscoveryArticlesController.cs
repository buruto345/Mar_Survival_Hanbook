using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Discovery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Controllers.Admin;

[ApiController, Route("api/admin/discover-mars/articles"), Authorize(Policy = "Cms")]
public sealed class DiscoveryArticlesController(MarsDbContext db, ArticleService service, DiscoveryService discovery, CurrentActor actor) : ControllerBase
{
    private static object Dto(Article a) => new { a.Id, a.Title, a.Slug, a.Summary, a.CategoryId, a.LanguageCode,
        a.Status, a.PublishedVersionId, a.CurrentDraftVersionId, a.RequiresReview, a.CreatedById, a.ReviewedAt, a.PublishedAt };
    private IActionResult Result(Article a) { Response.Headers.ETag = Concurrency.ETag(a); return Ok(Dto(a)); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery queryOptions, string? category,
        ArticleStatus? publicationStatus, ArticleVersionReviewState? reviewState, bool? requiresReview, CancellationToken ct)
    {
        var query = db.Articles.AsNoTracking().Where(x => x.Category.Module.Slug == "discover-mars" &&
            (actor.IsAdmin || actor.IsReviewer || x.CreatedById == actor.Id));
        if (category != null) query = query.Where(x => x.Category.Slug == category);
        if (publicationStatus.HasValue) query = query.Where(x => x.Status == publicationStatus);
        if (requiresReview.HasValue) query = query.Where(x => x.RequiresReview == requiresReview);
        if (reviewState.HasValue) query = query.Where(x => db.ArticleVersions.Any(v => v.Id == x.CurrentDraftVersionId && v.ReviewState == reviewState));
        if (queryOptions.Search != null) query = query.Where(x => x.Title.Contains(queryOptions.Search));
        var page = await query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ThenBy(x => x.Id).PageAsync(queryOptions, ct);
        return Ok(new PageResult<object>(page.Items.Select(Dto).ToArray(), page.Page, page.PageSize, page.Total));
    }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Result(await service.GetAsync(id, ct));
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(ArticleDraftRequest r, CancellationToken ct)
    {
        var a = await service.SaveDraftAsync(null, r, null, ct); Response.Headers.ETag = Concurrency.ETag(a);
        return CreatedAtAction(nameof(Get), new { id = a.Id }, Dto(a));
    }
    [HttpPut("{id:guid}/draft"), Authorize(Policy = "Edit")] public async Task<IActionResult> Draft(Guid id, ArticleDraftRequest r, CancellationToken ct)
    {
        var a = await service.SaveDraftAsync(id, r, Request.Headers.IfMatch, ct); Response.Headers.ETag = Concurrency.ETag(a);
        return CreatedAtAction(nameof(Version), new { id, versionId = a.CurrentDraftVersionId }, Dto(a));
    }
    [HttpGet("{id:guid}/versions")] public async Task<IActionResult> Versions(Guid id, [FromQuery] PageQuery queryOptions, CancellationToken ct)
    {
        _ = await service.GetAsync(id, ct);
        return Ok(await db.ArticleVersions.Where(x => x.ArticleId == id).OrderByDescending(x => x.Version)
            .Select(x => new { x.Id, x.Version, x.ReviewState, x.SnapshotSchemaVersion, x.SnapshotHash, x.ChangeSummary, x.ChangedAt, x.ReviewedAt }).PageAsync(queryOptions, ct));
    }
    [HttpGet("{id:guid}/versions/{versionId:guid}")] public async Task<IActionResult> Version(Guid id, Guid versionId, CancellationToken ct)
    {
        var v = await service.VersionAsync(id, versionId, ct); Response.Headers.ETag = Concurrency.ETag(v);
        return Ok(new { v.Id, v.ArticleId, v.Version, v.ReviewState, v.SnapshotHash, v.SnapshotSchemaVersion,
            Snapshot = System.Text.Json.JsonDocument.Parse(v.Snapshot).RootElement, v.ChangedAt, v.ReviewedAt, v.ReviewedById });
    }
    [HttpGet("{id:guid}/versions/{versionId:guid}/preview")] public async Task<IActionResult> Preview(Guid id, Guid versionId, CancellationToken ct)
        => Ok(await discovery.RenderAsync(await service.GetAsync(id, ct), await service.VersionAsync(id, versionId, ct), false, ct));
    [HttpPost("{id:guid}/versions/{versionId:guid}/submit"), Authorize(Policy = "Edit")]
    public async Task<IActionResult> Submit(Guid id, Guid versionId, CancellationToken ct)
    {
        var t = await service.SubmitAsync(id, versionId, Request.Headers.IfMatch, ct); Response.Headers.ETag = Concurrency.ETag(t);
        return Created("/api/admin/discover-mars/reviews/" + t.Id, DiscoveryReviewsController.Dto(t));
    }
    [HttpPost("{id:guid}/publish"), Authorize(Policy = "Admin")]
    public async Task<IActionResult> Publish(Guid id, VersionRequest r, CancellationToken ct) => Result(await service.PublishAsync(id, r.VersionId, Request.Headers.IfMatch, ct));
    [HttpPost("{id:guid}/unpublish"), Authorize(Policy = "Admin")]
    public async Task<IActionResult> Unpublish(Guid id, ReasonRequest r, CancellationToken ct) => Result(await service.UnpublishAsync(id, r.Reason, Request.Headers.IfMatch, ct));
    [HttpPost("{id:guid}/restore"), Authorize(Policy = "Edit")]
    public async Task<IActionResult> Restore(Guid id, RestoreRequest r, CancellationToken ct)
    {
        var a = await service.RestoreAsync(id, r, Request.Headers.IfMatch, ct); Response.Headers.ETag = Concurrency.ETag(a);
        return CreatedAtAction(nameof(Version), new { id, versionId = a.CurrentDraftVersionId }, Dto(a));
    }
}
