using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Editorial;

public sealed class ReviewService(MarsDbContext db, CurrentActor actor, SnapshotService snapshots)
{
    public async Task<ReviewTask> ClaimAsync(Guid id, string? etag, CancellationToken ct)
    {
        var task = await db.ReviewTasks.Include(x => x.Article).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        Concurrency.Check(task, etag);
        if (task.State != ReviewState.Pending || task.ReviewerId != null) throw ApiException.Conflict("Task đã được nhận hoặc hoàn tất.");
        task.ReviewerId = actor.Id; task.State = ReviewState.InProgress;
        Audit.Add(db, actor.Id, nameof(ReviewTask), id, AuditAction.Updated, "Review claimed", actor.TraceId);
        await db.SaveChangesAsync(ct); return task;
    }
    public async Task<ReviewTask> DecideAsync(Guid id, ReviewDecision r, string? etag, CancellationToken ct)
    {
        if (r.Decision is not (ReviewState.Approved or ReviewState.ChangesRequested or ReviewState.Rejected))
            throw ApiException.Invalid("Quyết định review không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "publication", ct);
        var t = await db.ReviewTasks.Include(x => x.Article).Include(x => x.ArticleVersion).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.Missing();
        Concurrency.Check(t, etag);
        if (!actor.IsAdmin && t.ReviewerId != actor.Id)
            throw new ApiException(403, "review_not_assigned", "Task không được giao cho reviewer này.");
        if (t.State != ReviewState.InProgress || t.Article.CurrentDraftVersionId != t.ArticleVersionId ||
            t.ArticleVersion.ReviewState != ArticleVersionReviewState.InReview) throw ApiException.Conflict("Bản review đã được thay thế.");
        if (r.Decision == ReviewState.Approved)
        {
            if (!r.Checklist.Complete) throw ApiException.Gate("Cần hoàn thành checklist khoa học.");
            await snapshots.ValidateGateAsync(t.ArticleVersion, ct);
        }
        t.ReviewerId = actor.Id;
        t.State = r.Decision; t.ReviewNotes = r.Notes; t.CompletedAt = DateTimeOffset.UtcNow;
        t.SourcesChecked = r.Checklist.SourcesChecked; t.ClaimsChecked = r.Checklist.ClaimsChecked;
        t.UnitsChecked = r.Checklist.UnitsChecked; t.EvidenceChecked = r.Checklist.EvidenceChecked; t.MediaRightsChecked = r.Checklist.MediaRightsChecked;
        t.ArticleVersion.ReviewState = r.Decision switch {
            ReviewState.Approved => ArticleVersionReviewState.Approved,
            ReviewState.Rejected => ArticleVersionReviewState.Rejected,
            _ => ArticleVersionReviewState.ChangesRequested };
        t.ArticleVersion.ReviewedById = actor.Id; t.ArticleVersion.ReviewedAt = t.CompletedAt;
        Audit.Add(db, actor.Id, nameof(ReviewTask), id, r.Decision == ReviewState.Approved ? AuditAction.Approved : AuditAction.Rejected, r.Notes, actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return t;
    }
}
