using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Controllers.Admin;

[ApiController, Route("api/admin/discover-mars"), Authorize(Policy = "Cms")]
public sealed class DiscoverySourcesController(MarsDbContext db, SourceService service, RawStorage storage) : ControllerBase
{
    [HttpGet("sources")]
    public async Task<IActionResult> List([FromQuery] PageQuery queryOptions, SourceProvider? provider, VerificationStatus? verificationStatus, CancellationToken ct)
    {
        var query = db.ScientificSources.AsNoTracking().Where(x => (provider == null || x.Provider == provider) &&
            (verificationStatus == null || x.VerificationStatus == verificationStatus) && (queryOptions.Search == null || x.Title.Contains(queryOptions.Search)));
        return Ok(await query.OrderBy(x => x.Id).Select(x => new { x.Id, x.Provider, x.IdentityKey, x.Title, x.CurrentAcceptedRevisionId, x.VerificationStatus }).PageAsync(queryOptions, ct));
    }
    [HttpGet("sources/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var s = await db.ScientificSources.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        Response.Headers.ETag = Concurrency.ETag(s);
        return Ok(new { s.Id, s.Provider, s.IdentityKey, s.ExternalRecordId, s.Title, s.CurrentAcceptedRevisionId, s.LastCheckedAt });
    }
    [HttpGet("sources/{id:guid}/revisions")]
    public async Task<IActionResult> Revisions(Guid id, [FromQuery] PageQuery queryOptions, CancellationToken ct)
    {
        if (!await db.ScientificSources.AnyAsync(x => x.Id == id, ct)) throw ApiException.Missing();
        return Ok(await db.SourceRevisions.Where(x => x.SourceId == id).OrderByDescending(x => x.RevisionNumber)
            .Select(x => new { x.Id, x.SourceId, x.RevisionNumber, x.Fingerprint, x.VerificationStatus, x.RevokedAt, x.RetrievedAt }).PageAsync(queryOptions, ct));
    }
    public static object RevisionDto(SourceRevision r) => new { r.Id, r.SourceId, r.RevisionNumber, r.ProviderVersion,
        r.ContentHash, r.Fingerprint, r.NormalizationVersion, Metadata = DiscoveryJson.Read<SourceMetadata>(r.MetadataSnapshot),
        r.RawChecksum, r.RetrievedAt, r.VerificationStatus, r.VerifiedById, r.VerifiedAt, r.VerificationNotes, r.RevokedAt, r.RevocationReason };
    [HttpGet("source-revisions/{id:guid}")]
    public async Task<IActionResult> Revision(Guid id, CancellationToken ct)
    {
        var r = await db.SourceRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        Response.Headers.ETag = Concurrency.ETag(r); return Ok(RevisionDto(r));
    }
    [HttpGet("source-revisions/{id:guid}/raw"), Authorize(Policy = "ReviewRead")]
    public async Task<IActionResult> Raw(Guid id, [FromQuery] string? blob, CancellationToken ct)
    {
        var r = await db.SourceRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        if (r.RawStoragePath is null) throw ApiException.Missing();
        var bytes = await storage.ReadAsync(r.RawStoragePath, ct);
        if (blob is null) return File(bytes, "application/json", "source-" + id + ".json");
        using var doc = JsonDocument.Parse(bytes);
        if (!doc.RootElement.TryGetProperty("files", out var files)) throw ApiException.Missing();
        var entry = files.EnumerateArray().FirstOrDefault(x => x.GetProperty("blob").GetString() == blob);
        if (entry.ValueKind == JsonValueKind.Undefined) throw ApiException.Missing();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(await storage.ReadAsync(blob, ct), entry.GetProperty("mimeType").GetString() ?? "application/octet-stream", blob);
    }
    [HttpPost("sources/manual"), Authorize(Policy = "Edit")]
    public async Task<IActionResult> Manual(ManualSourceRequest r, CancellationToken ct)
    {
        var (revision, created) = await service.ManualAsync(r, ct); Response.Headers.ETag = Concurrency.ETag(revision);
        return created ? CreatedAtAction(nameof(Revision), new { id = revision.Id }, RevisionDto(revision)) : Ok(RevisionDto(revision));
    }
    [HttpPost("source-revisions/{id:guid}/decision"), Authorize(Policy = "Review")]
    public async Task<IActionResult> Decision(Guid id, SourceDecision request, CancellationToken ct)
    {
        var r = await service.DecideAsync(id, request, Request.Headers.IfMatch, ct);
        Response.Headers.ETag = Concurrency.ETag(r); return Ok(RevisionDto(r));
    }
    [HttpPost("source-revisions/{id:guid}/amend"), Authorize(Policy = "Edit")]
    public async Task<IActionResult> Amend(Guid id, SourceAmendmentRequest request, CancellationToken ct)
    {
        var (revision, created) = await service.AmendAsync(id, request, Request.Headers.IfMatch, ct);
        Response.Headers.ETag = Concurrency.ETag(revision);
        return created ? CreatedAtAction(nameof(Revision), new { id = revision.Id }, RevisionDto(revision)) : Ok(RevisionDto(revision));
    }
    [HttpPost("source-revisions/{id:guid}/revoke"), Authorize(Policy = "Review")]
    public async Task<IActionResult> Revoke(Guid id, ReasonRequest request, CancellationToken ct)
    {
        var r = await service.RevokeAsync(id, request.Reason, Request.Headers.IfMatch, ct);
        Response.Headers.ETag = Concurrency.ETag(r); return Ok(RevisionDto(r));
    }
}
