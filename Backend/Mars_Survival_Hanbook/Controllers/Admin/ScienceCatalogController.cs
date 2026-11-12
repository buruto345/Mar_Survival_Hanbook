using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Controllers.Admin;

public record TechnologyIdentityRequest([Required, MaxLength(200)] string Name,
    [Required, RegularExpression("^[a-z0-9-]{1,200}$")] string Slug, [Required, MaxLength(4000)] string Description);
public record TechnologyIdentity(Guid Id, string Name, string Slug, Guid? ArticleId);

public record TechnologySuggestions(Guid Id, string Name, string Slug, string Description, string? ProcessDescription,
    string Capabilities, string Limitations, TechnologyStatus TechnologyStatus, EvidenceType EvidenceType, TestEnvironment TestEnvironment,
    ProfileResource[] Resources, ProfileHazard[] Hazards, Guid[] FactIds, Guid[] SourceRevisionIds);
public record CatalogOption(Guid Id, string Slug, string Label, string? Description);

[ScienceTechnology, ApiController, Authorize(Policy = "Edit"), Route("api/admin/science-technology/catalog")]
public sealed class ScienceCatalogController(MarsDbContext db, CurrentActor actor) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<TechnologyIdentity>> List([FromQuery] PageQuery query, CancellationToken ct) =>
        db.Technologies.Where(t => t.ArchivedAt == null && (query.Search == null || t.Name.Contains(query.Search)))
            .OrderBy(t => t.Name).ThenBy(t => t.Id).Select(t => new TechnologyIdentity(t.Id, t.Name, t.Slug,
                db.ScienceTechnologyProfiles.Where(p => p.TechnologyId == t.Id).Select(p => (Guid?)p.ArticleId).SingleOrDefault())).PageAsync(query, ct);

    [HttpGet("resources")]
    public Task<PageResult<CatalogOption>> Resources([FromQuery] PageQuery query, CancellationToken ct) =>
        db.Resources.Where(r => r.ArchivedAt == null && (query.Search == null || r.Name.Contains(query.Search)))
            .OrderBy(r => r.Name).ThenBy(r => r.Id).Select(r => new CatalogOption(r.Id, r.Slug, r.Name, r.Description)).PageAsync(query, ct);

    [HttpGet("hazards")]
    public Task<PageResult<CatalogOption>> Hazards([FromQuery] PageQuery query, CancellationToken ct) =>
        db.Hazards.Where(h => h.ArchivedAt == null && (query.Search == null || h.Name.Contains(query.Search)))
            .OrderBy(h => h.Name).ThenBy(h => h.Id).Select(h => new CatalogOption(h.Id, h.Slug, h.Name, h.Description)).PageAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TechnologySuggestions>> Suggestions(Guid id, CancellationToken ct)
    {
        var result = await db.Technologies.Where(t => t.Id == id && t.ArchivedAt == null).Select(t => new TechnologySuggestions(
            t.Id, t.Name, t.Slug, t.Description, t.ProcessDescription, t.Capabilities, t.Limitations,
            t.Status, t.EvidenceType, t.EnvironmentTested,
            t.Resources.Where(r => r.Resource.ArchivedAt == null).Select(r => new ProfileResource(r.ResourceId, r.Direction, r.Resource.Name, r.ProcessNote)).ToArray(),
            t.Hazards.Where(h => h.Hazard.ArchivedAt == null).Select(h => new ProfileHazard(h.HazardId, h.Hazard.Name, h.MitigationDescription)).ToArray(),
            t.Facts.Where(f => f.Fact.ArchivedAt == null).Select(f => f.FactId).ToArray(),
            t.Sources.Where(s => s.Source.CurrentAcceptedRevisionId != null).Select(s => s.Source.CurrentAcceptedRevisionId!.Value).ToArray())).SingleOrDefaultAsync(ct) ?? throw ApiException.Missing();
        return Ok(result);
    }

    [HttpPost, ProducesResponseType(typeof(TechnologyIdentity), StatusCodes.Status201Created)]
    public async Task<ActionResult<TechnologyIdentity>> Create(TechnologyIdentityRequest request, CancellationToken ct)
    {
        var technology = new Technology { Name = request.Name, Slug = request.Slug, Description = request.Description,
            Capabilities = "", Limitations = "" };
        db.Technologies.Add(technology);
        Audit.Add(db, actor.Id, nameof(Technology), technology.Id, AuditAction.Created, "Created technology identity", actor.TraceId);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Suggestions), new { technology.Id }, new TechnologyIdentity(technology.Id, technology.Name, technology.Slug, null));
    }
}
