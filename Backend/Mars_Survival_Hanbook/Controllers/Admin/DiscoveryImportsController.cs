using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Ingestion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Controllers.Admin;

[ApiController, Route("api/admin/discover-mars"), Authorize(Policy = "Edit")]
public sealed class DiscoveryImportsController(MarsDbContext db, ImportService service, CurrentActor actor) : ControllerBase
{
    private static object JobDto(IngestionJob j) => new { j.Id, JobId = j.Id, j.ConnectorId, j.Status, j.StartedAt, j.CompletedAt,
        j.RecordsFetched, j.RecordsAccepted, j.RecordsUnchanged, j.RecordsRejected, j.RecordsFailed, j.ErrorMessage,
        j.AttemptCount, j.CheckpointJson, j.LeaseExpiresAt, j.NextAttemptAt };
    [HttpPost("imports")] public async Task<IActionResult> Create(ImportRequest r, CancellationToken ct)
    {
        var j = await service.CreateAsync(r, Request.Headers["Idempotency-Key"], ct); Response.Headers.ETag = Concurrency.ETag(j);
        return AcceptedAtAction(nameof(Get), new { jobId = j.Id }, JobDto(j));
    }
    [HttpGet("imports")] public async Task<IActionResult> List([FromQuery] PageQuery queryOptions, CancellationToken ct)
    {
        var p = await db.IngestionJobs.Where(x => actor.IsAdmin || x.RequestedById == actor.Id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).PageAsync(queryOptions, ct);
        return Ok(new PageResult<object>(p.Items.Select(JobDto).ToArray(), p.Page, p.PageSize, p.Total));
    }
    [HttpGet("imports/{jobId:guid}")] public async Task<IActionResult> Get(Guid jobId, CancellationToken ct)
    { var j = await service.GetAsync(jobId, ct); Response.Headers.ETag = Concurrency.ETag(j); return Ok(JobDto(j)); }
    [HttpGet("imports/{jobId:guid}/items")] public async Task<IActionResult> Items(Guid jobId, [FromQuery] PageQuery queryOptions, IngestionItemStatus? status, CancellationToken ct)
    {
        _ = await service.GetAsync(jobId, ct);
        return Ok(await db.IngestionItems.Where(x => x.JobId == jobId && (status == null || x.Status == status)).OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.ItemKey, x.Status, x.SourceId, x.SourceRevisionId, x.AttemptCount, x.NextAttemptAt, x.ErrorCode, x.ErrorMessage, x.NormalizedPayload }).PageAsync(queryOptions, ct));
    }
    [HttpPost("imports/{jobId:guid}/retry")] public async Task<IActionResult> Retry(Guid jobId, CancellationToken ct)
    {
        var j = await service.RetryAsync(jobId, Request.Headers.IfMatch, ct); Response.Headers.ETag = Concurrency.ETag(j);
        return AcceptedAtAction(nameof(Get), new { jobId }, JobDto(j));
    }
}

[ApiController, Route("api/admin/discover-mars/connectors"), Authorize(Policy = "Cms")]
public sealed class DiscoveryConnectorsController(MarsDbContext db, CurrentActor actor, NasaUrlPolicy urls) : ControllerBase
{
    private static object Dto(NasaConnector c) => new { c.Id, c.Key, c.Name, c.ConnectorType, c.BaseUrl, c.IsEnabled, c.AccessPolicy, c.RateLimitPerMinute, c.LastSyncedAt, ETag = Concurrency.ETag(c) };
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok((await db.NasaConnectors.OrderBy(x => x.Key).ToListAsync(ct)).Select(Dto));
    [HttpGet("{key}")] public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var c = await db.NasaConnectors.SingleOrDefaultAsync(x => x.Key == key, ct) ?? throw ApiException.Missing();
        Response.Headers.ETag = Concurrency.ETag(c); return Ok(Dto(c));
    }
    [HttpPut("{key}"), Authorize(Policy = "Admin")] public async Task<IActionResult> Update(string key, ConnectorRequest r, CancellationToken ct)
    {
        var c = await db.NasaConnectors.SingleOrDefaultAsync(x => x.Key == key, ct) ?? throw ApiException.Missing();
        Concurrency.Check(c, Request.Headers.IfMatch);
        if (!Uri.TryCreate(r.BaseUrl, UriKind.Absolute, out var uri)) throw ApiException.Invalid("Base URL không hợp lệ.");
        urls.Validate(uri);
        c.BaseUrl = r.BaseUrl; c.IsEnabled = r.IsEnabled; c.AccessPolicy = r.AccessPolicy; c.RateLimitPerMinute = r.RateLimitPerMinute;
        Audit.Add(db, actor.Id, nameof(NasaConnector), c.Id, AuditAction.Updated, "Connector configuration updated", actor.TraceId);
        await db.SaveChangesAsync(ct); Response.Headers.ETag = Concurrency.ETag(c); return Ok(Dto(c));
    }
}
