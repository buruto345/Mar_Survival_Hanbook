using System.Text.Json;
using System.Text.Json.Nodes;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Editorial;

public record WorkingResource(Entity Entity, JsonElement Data);
public sealed class ContentService(MarsDbContext db, CurrentActor actor)
{
    public static readonly string[] Kinds = ["facts", "measurements", "missions", "locations", "timeline-events", "media", "map-layers"];
    public async Task<WorkingResource> GetAsync(string kind, Guid id, CancellationToken ct) => kind switch
    {
        "facts" => await GetTypedAsync<ScientificFact>(id, ct),
        "measurements" => await GetTypedAsync<Measurement>(id, ct),
        "missions" => await GetTypedAsync<Mission>(id, ct),
        "locations" => await GetTypedAsync<MarsLocation>(id, ct),
        "timeline-events" => await GetTypedAsync<ExplorationEvent>(id, ct),
        "media" => await GetTypedAsync<MediaAsset>(id, ct),
        "map-layers" => await GetTypedAsync<MapLayer>(id, ct),
        _ => throw ApiException.Missing()
    };
    private async Task<WorkingResource> GetTypedAsync<T>(Guid id, CancellationToken ct) where T : Entity
    {
        var e = await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        return new(e, await RepresentationAsync(e, ct));
    }
    public async Task<PageResult<JsonElement>> ListAsync(string kind, PageQuery page, Guid? sourceId, CancellationToken ct) => kind switch
    {
        "facts" => await ListTypedAsync(db.Set<ScientificFact>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)).Where(x => page.Search == null || x.Name.Contains(page.Search)), page, ct),
        "measurements" => await ListTypedAsync(db.Set<Measurement>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)), page, ct),
        "missions" => await ListTypedAsync(db.Set<Mission>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)).Where(x => page.Search == null || x.Name.Contains(page.Search)), page, ct),
        "locations" => await ListTypedAsync(db.Set<MarsLocation>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)).Where(x => page.Search == null || x.Name.Contains(page.Search)), page, ct),
        "timeline-events" => await ListTypedAsync(db.Set<ExplorationEvent>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)).Where(x => page.Search == null || x.Title.Contains(page.Search)), page, ct),
        "media" => await ListTypedAsync(db.Set<MediaAsset>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)).Where(x => page.Search == null || x.Title.Contains(page.Search)), page, ct),
        "map-layers" => await ListTypedAsync(db.Set<MapLayer>().Where(x => x.ArchivedAt == null && (!sourceId.HasValue || x.SourceId == sourceId)).Where(x => page.Search == null || x.Name.Contains(page.Search)), page, ct),
        _ => throw ApiException.Missing()
    };
    private async Task<PageResult<JsonElement>> ListTypedAsync<T>(IQueryable<T> query, PageQuery page, CancellationToken ct) where T : Entity
    {
        var result = await query.OrderBy(x => x.Id).PageAsync(page, ct);
        var items = new List<JsonElement>();
        foreach (var e in result.Items) items.Add(await RepresentationAsync(e, ct));
        return new(items, result.Page, result.PageSize, result.Total);
    }
    public async Task<WorkingResource> SaveAsync(string kind, Guid? id, ProvenancedRequest request, string? etag, CancellationToken ct)
    {
        ContentValidation.Validate(request);
        var revision = await db.SourceRevisions.SingleOrDefaultAsync(x => x.Id == request.SourceRevisionId, ct) ?? throw ApiException.Invalid("Revision không tồn tại.");
        if (revision.RevokedAt != null) throw ApiException.Gate("Revision đã bị thu hồi.");
        switch (request)
        {
            case FactRequest f:
                await ValidateUnitAsync(f.UnitId, f.OriginalValue, f.OriginalUnit, f.Value, ct); break;
            case MeasurementRequest m:
                await ValidateUnitAsync(m.UnitId, m.OriginalValue, m.OriginalUnit, m.Value, ct);
                if (!await db.ScientificFacts.AnyAsync(x => x.Id == m.FactId && x.ArchivedAt == null, ct)) throw ApiException.Invalid("Fact không tồn tại.");
                if (m.MissionId.HasValue && !await db.Missions.AnyAsync(x => x.Id == m.MissionId && x.ArchivedAt == null, ct)) throw ApiException.Invalid("Mission không tồn tại.");
                if (m.LocationId.HasValue && !await db.MarsLocations.AnyAsync(x => x.Id == m.LocationId && x.ArchivedAt == null, ct)) throw ApiException.Invalid("Location không tồn tại.");
                break;
            case TimelineEventRequest e when e.MissionId.HasValue:
                if (!await db.Missions.AnyAsync(x => x.Id == e.MissionId && x.ArchivedAt == null, ct)) throw ApiException.Invalid("Mission không tồn tại."); break;
            case MapLayerRequest m when m.LocationId.HasValue:
                if (!await db.MarsLocations.AnyAsync(x => x.Id == m.LocationId && x.ArchivedAt == null, ct)) throw ApiException.Invalid("Location không tồn tại."); break;
        }
        Entity candidate = request switch
        {
            FactRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            MeasurementRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            MissionRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            LocationRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            TimelineEventRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            MediaRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            MapLayerRequest r => ContentMapping.ToEntity(r, revision.SourceId),
            _ => throw ApiException.Invalid("Loại nội dung không hợp lệ.")
        };
        if (id.HasValue)
        {
            var old = (await GetAsync(kind, id.Value, ct)).Entity;
            Concurrency.Check(old, etag);
            if (old.ArchivedAt != null) throw ApiException.Conflict("Nội dung đã lưu trữ.");
            if (old is ScientificFact { VerificationStatus: VerificationStatus.Accepted })
                throw ApiException.Conflict("Tạo candidate mới cho fact đã được xác minh.");
            var entry = db.Entry(old);
            foreach (var property in db.Entry(candidate).Properties)
                if (property.Metadata.Name is not ("Id" or "CreatedAt" or "UpdatedAt" or "RowVersion" or "ArchivedAt"))
                    entry.Property(property.Metadata.Name).CurrentValue = property.CurrentValue;
            candidate = old;
            foreach (var a in await db.Articles.Where(a => db.PublicationResources.Any(p =>
                p.ArticleVersionId == a.CurrentDraftVersionId && p.ResourceId == id)).ToListAsync(ct)) a.RequiresReview = true;
        }
        else db.Add(candidate);
        Audit.Add(db, actor.Id, kind, candidate.Id, id.HasValue ? AuditAction.Updated : AuditAction.Created, "Working content saved", actor.TraceId);
        await db.SaveChangesAsync(ct);
        return new(candidate, await RepresentationAsync(candidate, ct));
    }
    private async Task ValidateUnitAsync(Guid id, decimal original, string from, decimal value, CancellationToken ct)
    {
        var unit = await db.Units.SingleOrDefaultAsync(x => x.Id == id && x.ArchivedAt == null, ct) ?? throw ApiException.Invalid("Unit không tồn tại.");
        if (ContentValidation.ConvertUnit(original, from, unit.Symbol) != value)
            throw ApiException.Invalid("Giá trị không khớp quy đổi đơn vị gốc.");
    }
    public async Task<WorkingResource> ArchiveAsync(string kind, Guid id, string? etag, CancellationToken ct)
    {
        var result = await GetAsync(kind, id, ct); Concurrency.Check(result.Entity, etag);
        result.Entity.ArchivedAt = DateTimeOffset.UtcNow;
        Audit.Add(db, actor.Id, kind, id, AuditAction.Archived, "Working content archived", actor.TraceId);
        await db.SaveChangesAsync(ct);
        return new(result.Entity, await RepresentationAsync(result.Entity, ct));
    }
    public async Task<JsonElement> RepresentationAsync(Entity e, CancellationToken ct)
    {
        ProvenancedRequest request = e switch
        {
            ScientificFact x => ContentMapping.ToRequest(x),
            Measurement x => ContentMapping.ToRequest(x),
            Mission x => ContentMapping.ToRequest(x),
            MarsLocation x => ContentMapping.ToRequest(x),
            ExplorationEvent x => ContentMapping.ToRequest(x),
            MediaAsset x => ContentMapping.ToRequest(x),
            MapLayer x => ContentMapping.ToRequest(x),
            _ => throw ApiException.Invalid("Loại nội dung không được hỗ trợ.")
        };
        var json = JsonSerializer.SerializeToNode(request, request.GetType(), DiscoveryJson.Options)!.AsObject();
        json["id"] = e.Id;
        json["sourceId"] = await db.SourceRevisions.Where(x => x.Id == request.SourceRevisionId).Select(x => (Guid?)x.SourceId).SingleOrDefaultAsync(ct);
        Guid? unitId = e switch { ScientificFact f => f.UnitId, Measurement m => m.UnitId, _ => null };
        if (unitId.HasValue)
        {
            var unit = await db.Units.Where(x => x.Id == unitId).Select(x => new { x.Id, x.Symbol, x.QuantityKind }).SingleAsync(ct);
            json["unit"] = JsonSerializer.SerializeToNode(unit, DiscoveryJson.Options);
        }
        return JsonSerializer.SerializeToElement(json, DiscoveryJson.Options);
    }
}
