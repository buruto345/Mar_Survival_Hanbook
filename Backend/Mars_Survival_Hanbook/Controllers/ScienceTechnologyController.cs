using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Discovery;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Mvc;

namespace Mars_Survival_Hanbook.Controllers;

[ScienceTechnology, Route("api/science-technology")]
public sealed class ScienceTechnologyController(DiscoveryService service, RawStorage raw) : PublicationControllerBase(service, raw)
{
    [HttpGet("technologies")]
    public Task<PageResult<TechnologyCard>> Technologies([FromQuery] PageQuery query, string? category, CancellationToken ct)
        => Service.TechnologyCardsAsync(query, category, ct);

    [HttpGet("technologies/{slug}")]
    public Task<object> Technology(string slug, CancellationToken ct) => Service.ArticleAsync(slug, "en", ct);
}
