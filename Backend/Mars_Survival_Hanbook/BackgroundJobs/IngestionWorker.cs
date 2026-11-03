using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;
using Mars_Survival_Hanbook.Services;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.BackgroundJobs;

public sealed class WorkerHeartbeat
{
    public DateTimeOffset LastPoll { get; set; }
}
public sealed class IngestionWorker(IServiceScopeFactory scopes, IConfiguration config,
    WorkerHeartbeat heartbeat, ILogger<IngestionWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Nasa:WorkerEnabled", true)) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                heartbeat.LastPoll = DateTimeOffset.UtcNow;
                using var scope = scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IngestionProcessor>();
                if (await processor.RunOneAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { log.LogError(e, "Ingestion worker iteration failed"); }
            try { await Task.Delay(TimeSpan.FromSeconds(config.GetValue("Nasa:PollSeconds", 5)), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
public sealed class IngestionProcessor(MarsDbContext db, NasaConnectorClient nasa,
    SourceService sources, ILogger<IngestionProcessor> log)
{
    public async Task<bool> RunOneAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var id = await db.IngestionJobs.Where(x =>
            (x.Status == IngestionStatus.Queued || x.Status == IngestionStatus.Running) &&
            (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
            (x.LeaseExpiresAt == null || x.LeaseExpiresAt < now)).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (id is null) return false;
        var owner = Guid.NewGuid().ToString("N");
        var claimed = await db.IngestionJobs.Where(x => x.Id == id && (x.LeaseExpiresAt == null || x.LeaseExpiresAt < now) &&
            (x.Status == IngestionStatus.Queued || x.Status == IngestionStatus.Running))
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.LeaseOwner, owner).SetProperty(x => x.LeaseExpiresAt, now.AddMinutes(10))
                .SetProperty(x => x.Status, IngestionStatus.Running).SetProperty(x => x.StartedAt, x => x.StartedAt ?? now)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
        if (claimed == 0) return true;
        var job = await db.IngestionJobs.Include(x => x.Connector).SingleAsync(x => x.Id == id, ct);
        try
        {
            if (!job.Connector.IsEnabled) throw new UpstreamException("connector_disabled", false);
            var request = DiscoveryJson.Read<ImportRequest>(job.RequestJson!);
            while (job.CheckpointJson != "done")
            {
                var page = await ReadPayloadAsync(() => nasa.DiscoverAsync(job.Connector, request, job.CheckpointJson, ct));
                await RenewAsync(job, owner, ct);
                var known = await db.IngestionItems.Where(x => x.JobId == job.Id).Select(x => x.ItemKey).ToListAsync(ct);
                foreach (var key in page.Keys.Where(x => !known.Contains(x)).Distinct(StringComparer.Ordinal).Take(Math.Max(0, request.MaxItems - known.Count)))
                    db.IngestionItems.Add(new() { JobId = job.Id, ItemKey = key, Provider = NasaConnectorClient.Provider(job.Connector.ConnectorType) });
                job.CheckpointJson = page.NextCheckpoint ?? "done";
                await db.SaveChangesAsync(ct);
                if (await db.IngestionItems.CountAsync(x => x.JobId == job.Id, ct) >= request.MaxItems)
                { job.CheckpointJson = "done"; await db.SaveChangesAsync(ct); }
            }
            var items = await db.IngestionItems.Where(x => x.JobId == job.Id &&
                (x.Status == IngestionItemStatus.Queued || x.Status == IngestionItemStatus.Processing ||
                 x.Status == IngestionItemStatus.Failed && x.NextAttemptAt != null && x.NextAttemptAt <= now)).OrderBy(x => x.CreatedAt).ToListAsync(ct);
            foreach (var item in items)
            {
                await RenewAsync(job, owner, ct);
                item.Status = IngestionItemStatus.Processing; item.AttemptCount++;
                await db.SaveChangesAsync(ct);
                try
                {
                    var candidate = await ReadPayloadAsync(() => nasa.FetchAsync(job.Connector, item.ItemKey, ct));
                    await RenewAsync(job, owner, ct);
                    await sources.StoreAsync(candidate, item, job.RequestedById, ct);
                }
                catch (Exception e) when (IsPayloadError(e))
                {
                    var error = e as UpstreamException ?? new UpstreamException("invalid_schema", false);
                    var rejected = error.Code.StartsWith("policy_", StringComparison.Ordinal);
                    item.Status = rejected ? IngestionItemStatus.Rejected : IngestionItemStatus.Failed;
                    item.ErrorCode = error.Code; item.ErrorMessage = error.Code;
                    item.CompletedAt = DateTimeOffset.UtcNow;
                    item.NextAttemptAt = error.Retryable && item.AttemptCount < 3 ? RetryAt(error, item.AttemptCount) : null;
                    await db.SaveChangesAsync(ct);
                    log.LogWarning("Import item {JobId}/{ItemId} failed {Code}", job.Id, item.Id, error.Code);
                }
                if (job.Connector.RateLimitPerMinute is { } rate)
                    await Task.Delay(TimeSpan.FromSeconds(60d / rate), ct);
            }
            await RecountAsync(job, ct);
            job.NextAttemptAt = await db.IngestionItems.Where(x => x.JobId == job.Id && x.Status == IngestionItemStatus.Failed)
                .MinAsync(x => x.NextAttemptAt, ct);
            if (job.NextAttemptAt.HasValue) job.Status = IngestionStatus.Queued;
            else
            {
                job.Status = job.RecordsFailed == 0 ? IngestionStatus.Succeeded :
                    job.RecordsAccepted + job.RecordsUnchanged + job.RecordsRejected > 0 ? IngestionStatus.PartiallySucceeded : IngestionStatus.Failed;
                job.CompletedAt = DateTimeOffset.UtcNow;
                Audit.Add(db, job.RequestedById, nameof(IngestionJob), job.Id,
                    job.Status == IngestionStatus.Failed ? AuditAction.ImportFailed : AuditAction.ImportCompleted, job.Status.ToString());
                job.Connector.LastSyncedAt = job.CompletedAt;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception e) when (IsPayloadError(e))
        {
            var error = e as UpstreamException ?? new UpstreamException("invalid_request_or_schema", false);
            job.ErrorMessage = error.Code;
            await RecountAsync(job, ct);
            job.NextAttemptAt = error.Retryable && job.AttemptCount < 3 ? RetryAt(error, job.AttemptCount) : null;
            job.Status = job.NextAttemptAt.HasValue ? IngestionStatus.Queued : IngestionStatus.Failed;
            job.CompletedAt = job.NextAttemptAt.HasValue ? null : DateTimeOffset.UtcNow;
        }
        job.LeaseOwner = null; job.LeaseExpiresAt = null;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Import {JobId} {Status}: {Fetched} fetched, {Created} created, {Unchanged} unchanged, {Failed} failed",
            job.Id, job.Status, job.RecordsFetched, job.RecordsAccepted, job.RecordsUnchanged, job.RecordsFailed);
        return true;
    }
    private async Task RenewAsync(IngestionJob job, string owner, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (await db.IngestionJobs.Where(x => x.Id == job.Id && x.LeaseOwner == owner && x.LeaseExpiresAt > now)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.LeaseExpiresAt, now.AddMinutes(10)), ct) != 1)
            throw new InvalidOperationException("Worker lost its lease.");
        await db.Entry(job).ReloadAsync(ct);
    }
    private async Task RecountAsync(IngestionJob job, CancellationToken ct)
    {
        var counts = await db.IngestionItems.Where(x => x.JobId == job.Id).GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        job.RecordsFetched = counts.Values.Sum();
        job.RecordsAccepted = counts.GetValueOrDefault(IngestionItemStatus.RevisionCreated);
        job.RecordsUnchanged = counts.GetValueOrDefault(IngestionItemStatus.Unchanged);
        job.RecordsRejected = counts.GetValueOrDefault(IngestionItemStatus.Rejected);
        job.RecordsFailed = counts.GetValueOrDefault(IngestionItemStatus.Failed);
    }
    private static DateTimeOffset RetryAt(UpstreamException error, int attempts) => DateTimeOffset.UtcNow +
        (error.RetryAfter is { TotalSeconds: > 0 } delay ? TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds, 86400)) :
            TimeSpan.FromSeconds(Math.Pow(2, attempts) + Random.Shared.NextDouble()));
    private static bool IsPayloadError(Exception error) => error is UpstreamException or System.Text.Json.JsonException
        or System.Xml.XmlException or FormatException or OverflowException or KeyNotFoundException or ApiException;
    private static async Task<T> ReadPayloadAsync<T>(Func<Task<T>> read)
    {
        try { return await read(); }
        catch (Exception error) when (error is InvalidOperationException or ArgumentOutOfRangeException)
        { throw new UpstreamException("invalid_schema", false); }
    }
}
