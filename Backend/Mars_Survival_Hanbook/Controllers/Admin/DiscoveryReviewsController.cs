using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Controllers.Admin;

[ApiController, Authorize(Policy = "ReviewRead")]
public abstract class ReviewsControllerBase(ReviewService service) : ControllerBase
{
    public static object Dto(ReviewTask t) => new { t.Id, t.ArticleId, t.ArticleVersionId, t.State, t.ReviewerId,
        t.SubmittedById, t.SubmittedAt, t.CompletedAt, t.DueAt, t.ReviewNotes,
        Checklist = new ReviewChecklist(t.SourcesChecked, t.ClaimsChecked, t.UnitsChecked, t.EvidenceChecked, t.MediaRightsChecked) };
    private object ResponseDto(ReviewTask t) => HttpContext.GetEndpoint()?.Metadata.GetMetadata<LivingOnMarsAttribute>() == null
        ? Dto(t) : new { t.Id, t.ArticleId, t.ArticleVersionId, t.State, t.ReviewerId, t.SubmittedAt, t.CompletedAt };
    private IActionResult Result(ReviewTask t) { Response.Headers.ETag = Concurrency.ETag(t); return Ok(ResponseDto(t)); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery queryOptions, ReviewState? state, Guid? reviewerId, Guid? articleId, DateTimeOffset? dueBefore, CancellationToken ct)
    {
        var page = await service.List(queryOptions.Language).Where(x => (state == null || x.State == state) && (reviewerId == null || x.ReviewerId == reviewerId) &&
            (articleId == null || x.ArticleId == articleId) && (dueBefore == null || x.DueAt <= dueBefore))
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id).PageAsync(queryOptions, ct);
        return Ok(new PageResult<object>(page.Items.Select(ResponseDto).ToArray(), page.Page, page.PageSize, page.Total));
    }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Result(await service.Tasks.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing());
    [HttpPost("{id:guid}/claim"), Authorize(Policy = "Review")] public async Task<IActionResult> Claim(Guid id, CancellationToken ct)
        => Result(await service.ClaimAsync(id, Request.Headers.IfMatch, ct));
    [HttpPost("{id:guid}/decision"), Authorize(Policy = "Review")] public async Task<IActionResult> Decision(Guid id, ReviewDecision r, CancellationToken ct)
        => Result(await service.DecideAsync(id, r, Request.Headers.IfMatch, ct));
}

[LivingOnMars, Route("api/admin/living-on-mars/reviews")]
public sealed class LivingReviewsController(ReviewService service) : ReviewsControllerBase(service);

[Route("api/admin/discover-mars/reviews")]
public sealed class DiscoveryReviewsController(ReviewService service)
    : ReviewsControllerBase(service);

[SurvivalKnowledge, Route("api/admin/survival-knowledge/reviews")]
public sealed class SurvivalReviewsController(ReviewService service)
    : ReviewsControllerBase(service);
