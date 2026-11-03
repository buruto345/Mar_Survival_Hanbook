using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services.Discovery;
using Microsoft.AspNetCore.Mvc;
using Mars_Survival_Hanbook.Services.Sources;
using Mars_Survival_Hanbook.Services;
using System.Text.Json.Nodes;

namespace Mars_Survival_Hanbook.Controllers;

[ApiController, Route("api/discover-mars")]
public sealed class DiscoverMarsController(DiscoveryService service, RawStorage raw) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Module([FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await service.ModuleAsync(queryOptions.Language, ct));
    [HttpGet("categories")] public async Task<IActionResult> Categories([FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await service.CategoriesAsync(queryOptions.Language, ct));
    [HttpGet("categories/{slug}")] public async Task<IActionResult> Category(string slug, [FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await service.CategoryAsync(slug, queryOptions.Language, ct));
    [HttpGet("articles")] public async Task<IActionResult> Articles([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ArticlesAsync(queryOptions, ct));
    [HttpGet("articles/{slug}")] public async Task<IActionResult> Article(string slug, [FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await service.ArticleAsync(slug, queryOptions.Language, ct));
    [HttpGet("timeline")] public async Task<IActionResult> Timeline([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ResourcesAsync("timeline-events", queryOptions, ct));
    [HttpGet("missions")] public async Task<IActionResult> Missions([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ResourcesAsync("missions", queryOptions, ct));
    [HttpGet("locations")] public async Task<IActionResult> Locations([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ResourcesAsync("locations", queryOptions, ct));
    [HttpGet("facts")] public async Task<IActionResult> Facts([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ResourcesAsync("facts", queryOptions, ct));
    [HttpGet("measurements")] public async Task<IActionResult> Measurements([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ResourcesAsync("measurements", queryOptions, ct));
    [HttpGet("map-layers")] public async Task<IActionResult> MapLayers([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await service.ResourcesAsync("map-layers", queryOptions, ct));
    [HttpGet("missions/{id:guid}")] public async Task<IActionResult> Mission(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
        => Ok(await service.ResourceAsync("missions", id, articleVersionId, queryOptions.Language, ct));
    [HttpGet("locations/{id:guid}")] public async Task<IActionResult> Location(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
        => Ok(await service.ResourceAsync("locations", id, articleVersionId, queryOptions.Language, ct));
    [HttpGet("map-layers/{id:guid}")] public async Task<IActionResult> MapLayer(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
        => Ok(await service.ResourceAsync("map-layers", id, articleVersionId, queryOptions.Language, ct));
    [HttpGet("sources/{id:guid}/revisions/{revisionId:guid}")] public async Task<IActionResult> Source(Guid id, Guid revisionId, CancellationToken ct)
        => Ok(await service.SourceAsync(id, revisionId, ct));
    [HttpGet("media/{id:guid}/file")]
    public async Task<IActionResult> MediaFile(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
    {
        var media = (JsonObject)await service.ResourceAsync("media", id, articleVersionId, queryOptions.Language, ct);
        var checksum = media["fileChecksum"]?.GetValue<string>() ?? throw ApiException.Missing();
        var bytes = await raw.ReadAsync(checksum, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(bytes, media["mimeType"]?.GetValue<string>() ?? "application/octet-stream");
    }
}
