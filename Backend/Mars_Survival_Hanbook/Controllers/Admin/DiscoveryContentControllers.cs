using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;

namespace Mars_Survival_Hanbook.Controllers.Admin;

[ApiController, Route("api/admin/discover-mars/content/facts"), Authorize(Policy = "Cms")]
public sealed class FactContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("facts", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("facts", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(FactRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("facts", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, FactRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("facts", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("facts", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}

[ApiController, Route("api/admin/discover-mars/content/measurements"), Authorize(Policy = "Cms")]
public sealed class MeasurementContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("measurements", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("measurements", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(MeasurementRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("measurements", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, MeasurementRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("measurements", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("measurements", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}

[ApiController, Route("api/admin/discover-mars/content/missions"), Authorize(Policy = "Cms")]
public sealed class MissionContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("missions", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("missions", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(MissionRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("missions", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, MissionRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("missions", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("missions", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}

[ApiController, Route("api/admin/discover-mars/content/locations"), Authorize(Policy = "Cms")]
public sealed class LocationContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("locations", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("locations", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(LocationRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("locations", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, LocationRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("locations", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("locations", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}

[ApiController, Route("api/admin/discover-mars/content/timeline-events"), Authorize(Policy = "Cms")]
public sealed class TimelineEventContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("timeline-events", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("timeline-events", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(TimelineEventRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("timeline-events", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, TimelineEventRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("timeline-events", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("timeline-events", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}

[ApiController, Route("api/admin/discover-mars/content/media"), Authorize(Policy = "Cms")]
public sealed class MediaContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("media", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("media", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(MediaRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("media", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, MediaRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("media", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("media", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}

[ApiController, Route("api/admin/discover-mars/content/map-layers"), Authorize(Policy = "Cms")]
public sealed class MapLayerContentController(ContentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] Guid? sourceId, CancellationToken ct)
        => Ok(await service.ListAsync("map-layers", query, sourceId, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await service.GetAsync("map-layers", id, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost, Authorize(Policy = "Edit")] public async Task<IActionResult> Create(MapLayerRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("map-layers", null, request, null, ct); Response.Headers.ETag = Concurrency.ETag(r.Entity);
      return CreatedAtAction(nameof(Get), new { id = r.Entity.Id }, r.Data); }
    [HttpPut("{id:guid}"), Authorize(Policy = "Edit")] public async Task<IActionResult> Update(Guid id, MapLayerRequest request, CancellationToken ct)
    { var r = await service.SaveAsync("map-layers", id, request, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Edit")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    { var r = await service.ArchiveAsync("map-layers", id, Request.Headers.IfMatch, ct);
      Response.Headers.ETag = Concurrency.ETag(r.Entity); return Ok(r.Data); }
}
