using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Discovery;
using Mars_Survival_Hanbook.Services.Living;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Mvc;

namespace Mars_Survival_Hanbook.Controllers;

[LivingOnMars, Route("api/living-on-mars")]
public sealed class LivingOnMarsController(DiscoveryService discovery, RawStorage raw, LivingScenarioService scenarios)
    : KnowledgeControllerBase(discovery, raw)
{
    [HttpGet("scenarios")]
    public Task<object> Scenarios(CancellationToken ct) => scenarios.ListAsync(ct);

    [HttpGet("scenarios/{key}/versions/{version:int}")]
    public Task<ScenarioPack> Scenario(string key, int version, CancellationToken ct) => scenarios.ReadAsync(key, version, ct);

    [HttpPost("simulations/preview"), RequestSizeLimit(32768)]
    public async Task<PreviewResult> Preview(PreviewRequest request, CancellationToken ct) =>
        LivingSimulationService.Run(await scenarios.ReadAsync(request.ScenarioKey, request.ScenarioVersion, ct), request);
}
