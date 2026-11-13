using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services.Discovery;
using Microsoft.AspNetCore.Mvc;
using Mars_Survival_Hanbook.Services.Sources;
using Mars_Survival_Hanbook.Services;
using System.Text.Json.Nodes;

namespace Mars_Survival_Hanbook.Controllers;

[ApiController]
public abstract class PublicationControllerBase(DiscoveryService service, RawStorage raw) : ControllerBase
{
    protected DiscoveryService Service { get; } = service;
    [HttpGet] public async Task<IActionResult> Module([FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await Service.ModuleAsync(queryOptions.Language, ct));
    [HttpGet("categories")] public async Task<IActionResult> Categories([FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await Service.CategoriesAsync(queryOptions.Language, ct));
    [HttpGet("categories/{slug}")] public async Task<IActionResult> Category(string slug, [FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await Service.CategoryAsync(slug, queryOptions.Language, ct));
    [HttpGet("sources/{id:guid}/revisions/{revisionId:guid}")] public async Task<IActionResult> Source(Guid id, Guid revisionId, CancellationToken ct)
        => Ok(await Service.SourceAsync(id, revisionId, ct));
    [HttpGet("media/{id:guid}/file")]
    public async Task<IActionResult> MediaFile(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
    {
        // Older frozen URLs identify their version without repeating its language.
        var language = articleVersionId.HasValue && !Request.Query.ContainsKey("language") ? null : queryOptions.Language;
        var media = (JsonObject)await Service.ResourceAsync("media", id, articleVersionId, language, ct);
        var checksum = media["fileChecksum"]?.GetValue<string>() ?? throw ApiException.Missing();
        var bytes = await raw.ReadAsync(checksum, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(bytes, media["mimeType"]?.GetValue<string>() ?? "application/octet-stream");
    }
}

public abstract class KnowledgeControllerBase(DiscoveryService service, RawStorage raw) : PublicationControllerBase(service, raw)
{
    [HttpGet("articles")] public async Task<IActionResult> Articles([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ArticlesAsync(queryOptions, ct));
    [HttpGet("articles/{slug}")] public async Task<IActionResult> Article(string slug, [FromQuery] PageQuery queryOptions, CancellationToken ct) => Ok(await Service.ArticleAsync(slug, queryOptions.Language, ct));
}

[SurvivalKnowledge, Route("api/survival-knowledge")]
public sealed class SurvivalKnowledgeController(DiscoveryService service, RawStorage raw)
    : KnowledgeControllerBase(service, raw);

[HumanSurvival, Route("api/human-survival")]
public sealed class HumanSurvivalController(DiscoveryService service, RawStorage raw)
    : KnowledgeControllerBase(service, raw)
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(await Service.ModuleAsync("en", ct));
}

[Route("api/discover-mars")]
public sealed class DiscoverMarsController(DiscoveryService discovery, RawStorage raw, IWebHostEnvironment env)
    : KnowledgeControllerBase(discovery, raw)
{
    [HttpGet("categories/{slug}/image")]
    [Produces("image/jpeg")]
    public IActionResult CategoryImage(string slug)
    {
        var image = DiscoverCategoryImage.Find(slug);
        if (image is null) return NotFound();
        return PhysicalFile(Path.Combine(env.ContentRootPath, "ImageMarHanbook", image.FileName), "image/jpeg");
    }

    [HttpGet("timeline")] public async Task<IActionResult> Timeline([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ResourcesAsync("timeline-events", queryOptions, ct));
    [HttpGet("missions")] public async Task<IActionResult> Missions([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ResourcesAsync("missions", queryOptions, ct));
    [HttpGet("locations")] public async Task<IActionResult> Locations([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ResourcesAsync("locations", queryOptions, ct));
    [HttpGet("facts")] public async Task<IActionResult> Facts([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ResourcesAsync("facts", queryOptions, ct));
    [HttpGet("measurements")] public async Task<IActionResult> Measurements([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ResourcesAsync("measurements", queryOptions, ct));
    [HttpGet("map-layers")] public async Task<IActionResult> MapLayers([FromQuery] DiscoveryQuery queryOptions, CancellationToken ct) => Ok(await Service.ResourcesAsync("map-layers", queryOptions, ct));
    [HttpGet("missions/{id:guid}")] public async Task<IActionResult> Mission(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
        => Ok(await Service.ResourceAsync("missions", id, articleVersionId, queryOptions.Language, ct));
    [HttpGet("locations/{id:guid}")] public async Task<IActionResult> Location(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
        => Ok(await Service.ResourceAsync("locations", id, articleVersionId, queryOptions.Language, ct));
    [HttpGet("map-layers/{id:guid}")] public async Task<IActionResult> MapLayer(Guid id, Guid? articleVersionId, [FromQuery] PageQuery queryOptions, CancellationToken ct)
        => Ok(await Service.ResourceAsync("map-layers", id, articleVersionId, queryOptions.Language, ct));
}
