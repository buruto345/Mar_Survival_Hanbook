using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Ingestion;

public sealed class ImportService(MarsDbContext db, CurrentActor actor, IConfiguration config)
{
    public async Task<IngestionJob> CreateAsync(ImportRequest r, string? key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200) throw ApiException.Invalid("Supply an Idempotency-Key of at most 200 characters.");
        var c = await db.NasaConnectors.SingleOrDefaultAsync(x => x.Key == r.ConnectorKey && x.IsEnabled, ct)
            ?? throw ApiException.Invalid("The connector is missing or disabled.");
        if (r.MaxItems > config.GetValue("Nasa:Import:MaxItems", 500) || r.PageSize > r.MaxItems ||
            r.RecordIds.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 200))
            throw ApiException.Invalid("The import limits are invalid.");
        if (c.ConnectorType is ConnectorType.Images or ConnectorType.Ntrs &&
            r.RecordIds.Length == 0 && string.IsNullOrWhiteSpace(r.Query)) throw ApiException.Invalid("Supply a query or recordIds.");
        if (c.ConnectorType == ConnectorType.Ntrs && r.RecordIds.Any(x => x.Any(ch => !char.IsAsciiDigit(ch))))
            throw ApiException.Invalid("NTRS citation IDs must contain only digits.");
        if (c.ConnectorType == ConnectorType.Pds && (r.RecordIds.Length == 0 || r.RecordIds.Any(x => !x.StartsWith("urn:nasa:pds:"))))
            throw ApiException.Invalid("PDS requires an explicit LID or LIDVID.");
        if (c.ConnectorType == ConnectorType.Pds && r.IncludeMembers &&
            (!r.From.HasValue || !r.To.HasValue || r.To < r.From || (r.To.Value - r.From.Value).TotalDays > 31))
            throw ApiException.Invalid("Supply a PDS member date range of at most 31 days.");
        if (c.ConnectorType == ConnectorType.MarsTrek && (r.LayerKey == null || config["Nasa:MarsTrek:Layers:" + r.LayerKey] == null))
            throw ApiException.Invalid("The layer key has not been configured.");
        if (c.ConnectorType == ConnectorType.ManualArchive) throw ApiException.Invalid("Use /sources/manual for web sources.");
        _ = NasaConnectorClient.Provider(c.ConnectorType);
        var json = DiscoveryJson.Write(r); var hash = SourceNormalization.Hash(SourceNormalization.CanonicalJson(json));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "import:" + SourceNormalization.Hash(actor.Id + ":" + c.Id + ":" + key), ct);
        var existing = await db.IngestionJobs.SingleOrDefaultAsync(x => x.RequestedById == actor.Id && x.ConnectorId == c.Id && x.RequestKey == key, ct);
        if (existing is not null)
        {
            if (existing.RequestHash != hash) throw ApiException.Conflict("This Idempotency-Key has already been used for a different request.");
            await tx.CommitAsync(ct); return existing;
        }
        var job = new IngestionJob { ConnectorId = c.Id, RequestedById = actor.Id, RequestKey = key,
            RequestHash = hash, RequestJson = json, Endpoint = c.BaseUrl, ParserVersion = SourceNormalization.Version };
        db.IngestionJobs.Add(job);
        Audit.Add(db, actor.Id, nameof(IngestionJob), job.Id, AuditAction.ImportStarted, "Import queued", actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return job;
    }
    public async Task<IngestionJob> GetAsync(Guid id, CancellationToken ct)
    {
        var job = await db.IngestionJobs.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        actor.Own(job.RequestedById ?? Guid.Empty); return job;
    }
    public async Task<IngestionJob> RetryAsync(Guid id, string? etag, CancellationToken ct)
    {
        var job = await GetAsync(id, ct); Concurrency.Check(job, etag);
        if (job.Status is IngestionStatus.Running or IngestionStatus.Queued or IngestionStatus.Cancelled)
            throw ApiException.Conflict("The job cannot be retried in its current state.");
        foreach (var item in await db.IngestionItems.Where(x => x.JobId == id && x.Status == IngestionItemStatus.Failed && x.AttemptCount < 10).ToListAsync(ct))
        { item.Status = IngestionItemStatus.Queued; item.NextAttemptAt = null; item.CompletedAt = null; }
        job.Status = IngestionStatus.Queued; job.NextAttemptAt = null; job.LeaseOwner = null; job.LeaseExpiresAt = null;
        job.CompletedAt = null; job.ErrorMessage = null;
        Audit.Add(db, actor.Id, nameof(IngestionJob), id, AuditAction.ImportStarted, "Retry failed items", actor.TraceId);
        await db.SaveChangesAsync(ct); return job;
    }
}
