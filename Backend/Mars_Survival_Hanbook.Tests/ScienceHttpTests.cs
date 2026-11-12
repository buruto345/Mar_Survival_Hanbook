using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data.Science;
using Mars_Survival_Hanbook.Services.Editorial;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class ScienceHttpTests(DiscoveryHttpFixture fx) : IClassFixture<DiscoveryHttpFixture>
{
    private const string Root = "/api/science-technology";
    private const string Admin = "/api/admin/science-technology";
    private const string Articles = Admin + "/articles";
    private const string Sources = "/api/admin/discover-mars/source-revisions/";

    private async Task<string> Tag(string path) => (await fx.Admin.GetAsync(path)).Headers.ETag!.ToString();
    private async Task<HttpResponseMessage> Write(string path, string tag, object body, HttpMethod? method = null)
    {
        using var req = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(body, options: DiscoveryJson.Options) };
        req.Headers.TryAddWithoutValidation("If-Match", tag);
        return await fx.Admin.SendAsync(req);
    }
    private async Task<JsonElement> Success(HttpResponseMessage r)
    {
        var text = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri} {r.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    private async Task Publish(string path, Guid version)
    {
        var task = await Success(await Write($"{path}/versions/{version}/submit", await Tag(path), new { }));
        var review = Admin + "/reviews/" + task.GetProperty("id").GetGuid();
        await Success(await Write(review + "/claim", await Tag(review), new { }));
        await Success(await Write(review + "/decision", await Tag(review), new { decision = "Approved", notes = "Checked the complete English snapshot against the cited source.",
            checklist = new ReviewChecklist(true, true, true, true, true) }));
        var tag = await Tag(path);
        var responses = await Task.WhenAll(Write(path + "/publish", tag, new { versionId = version }),
            Write(path + "/publish", tag, new { versionId = version }));
        await Success(Assert.Single(responses, r => r.IsSuccessStatusCode));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.PreconditionFailed);
    }

    [SqlServerFact]
    public async Task ImportReviewPublishMoveRestoreAndRevoke()
    {
        // Fail on entry two after the first draft commits; retry the original pack twice.
        var pack = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "Science", "release.en.json")))!.AsArray();
        pack[1]!["category"] = "invalid";
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, pack.ToJsonString());
            await Assert.ThrowsAsync<Mars_Survival_Hanbook.Services.ApiException>(() => ScienceImport.RunAsync(fx.Services, file));
        }
        finally { File.Delete(file); }
        await ScienceImport.RunAsync(fx.Services);
        await ScienceImport.RunAsync(fx.Services);
        using var db = fx.Database.CreateContext();
        var versions = await db.ArticleVersions.AsNoTracking().Where(v => v.Article.Category.Module.Slug == "science-technology").ToListAsync();
        Assert.Equal(5, versions.Count);
        Assert.Equal(5, await db.ScienceTechnologyProfiles.CountAsync());
        var cats = await fx.Guest.GetFromJsonAsync<CategoryResponse[]>(Root + "/categories");
        Assert.Equal(5, cats!.Length);
        Assert.All(cats, c => Assert.Equal(0, c.PublishedArticleCount));
        Assert.Equal(HttpStatusCode.BadRequest, (await fx.Guest.GetAsync(Root + "?language=vi")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fx.Admin.GetAsync(Articles + "/" + fx.Database.ArticleId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fx.Viewer.GetAsync(Articles)).StatusCode);
        foreach (var version in versions)
        {
            var snap = SnapshotService.Read(version);
            var citation = Assert.Single(snap.Sources);
            var revision = Sources + citation.SourceRevisionId;
            await Success(await Write(revision + "/decision", await Tag(revision), new { decision = "Accepted", notes = "Checked the named primary source and section." }));
            var path = Articles + "/" + version.ArticleId;
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Write(path + "/publish", await Tag(path), new { versionId = version.Id })).StatusCode);
            await Publish(path, version.Id);
            await Success(await fx.Guest.GetAsync($"{Root}/sources/{citation.SourceId}/revisions/{citation.SourceRevisionId}"));
        }
        if (Environment.GetEnvironmentVariable("MARS_SCIENCE_EXAMPLES_DIR") is { Length: > 0 } examples)
        {
            Directory.CreateDirectory(examples);
            foreach (var (name, url) in new[] {
                ("water-cards", "/technologies?category=water-recycling-systems"),
                ("water-profile", "/technologies/iss-water-recovery-system") })
            {
                var sample = await fx.Guest.GetFromJsonAsync<JsonElement>(Root + url);
                await File.WriteAllTextAsync(Path.Combine(examples, name + ".json"),
                    JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
            }
        }
        // Changing the pack makes a draft; the reviewed edition remains public.
        pack[1]!["category"] = "in-situ-resource-utilization";
        pack[0]!["limitations"] = "Changed pack fixture.";
        var changedPack = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(changedPack, pack.ToJsonString());
            await ScienceImport.RunAsync(fx.Services, changedPack);
            await ScienceImport.RunAsync(fx.Services, changedPack);
            var water = await fx.Guest.GetFromJsonAsync<JsonElement>(Root + "/technologies/iss-water-recovery-system");
            Assert.NotEqual("Changed pack fixture.", water.GetProperty("technologyProfile").GetProperty("limitations").GetString());
            Assert.Equal(2, await db.ArticleVersions.CountAsync(v => v.Article.Slug == "iss-water-recovery-system"));
        }
        finally { File.Delete(changedPack); }
        var overview = await fx.Guest.GetFromJsonAsync<ModuleResponse>(Root);
        Assert.Equal(5, overview!.PublishedArticleCount);
        Assert.All(overview.Categories, c => Assert.Equal(1, c.PublishedArticleCount));
        var v1 = versions.Single(v => v.CategorySlug == "in-situ-resource-utilization");
        var s = SnapshotService.Read(v1);
        var article = Articles + "/" + v1.ArticleId;
        var pub = Root + "/technologies/moxie";
        var rev2 = SnapshotService.Read(versions.Single(v => v.CategorySlug == "robotics-and-ai")).Sources[0];
        var draft = new ArticleDraftRequest { Slug = "moxie", Title = s.Title, Summary = s.Summary, Sections = s.Sections,
            LanguageCode = "en", CategorySlug = "robotics-and-ai", Claims = s.Claims.Select(c => c with { SourceRevisionIds = [rev2.SourceRevisionId] }).ToArray(),
            TechnologyProfile = s.TechnologyProfile! with { Limitations = "Fixture revised boundary.", SourceRevisionIds = [rev2.SourceRevisionId] } };
        var identity = await Success(await fx.Admin.PostAsJsonAsync(Admin + "/catalog", new { name = "Fixture", slug = "fixture-tech", description = "Shared technology." }));
        var tech = identity.GetProperty("id").GetGuid();
        await Success(await fx.Admin.GetAsync(Admin + "/catalog/" + tech));
        var originalProfile = draft.TechnologyProfile;
        draft.TechnologyProfile = originalProfile with { TechnologyId = tech };
        draft.Slug = "race-a";
        var raceA = fx.Admin.PostAsJsonAsync(Articles, draft, DiscoveryJson.Options);
        var other = DiscoveryJson.Read<ArticleDraftRequest>(DiscoveryJson.Write(draft));
        other.Slug = "race-b";
        var race = await Task.WhenAll(raceA, fx.Admin.PostAsJsonAsync(Articles, other, DiscoveryJson.Options));
        Assert.Contains(race, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Contains(race, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await db.Articles.CountAsync(a => a.Slug == "race-a" || a.Slug == "race-b"));
        draft.Slug = "moxie";
        draft.TechnologyProfile = originalProfile;
        const string facts = "/api/admin/discover-mars/content/facts";
        var unit = await db.Units.SingleAsync(u => u.Symbol == "m");
        var fact = new FactRequest { SourceRevisionId = rev2.SourceRevisionId, Name = "Fixture clearance", Value = 1,
            OriginalValue = 1, UnitId = unit.Id, Context = "Bench fixture", MeasurementKind = Models.Enums.MeasurementKind.Single,
            EvidenceType = Models.Enums.EvidenceType.Tested, TestEnvironment = Models.Enums.TestEnvironment.Earth };
        Assert.Equal(HttpStatusCode.BadRequest, (await fx.Admin.PostAsJsonAsync(facts, fact, DiscoveryJson.Options)).StatusCode);
        fact.OriginalUnit = "m";
        var factId = (await Success(await fx.Admin.PostAsJsonAsync(facts, fact, DiscoveryJson.Options))).GetProperty("id").GetGuid();
        var factPath = facts + "/" + factId;
        await Success(await Write(factPath + "/decision", await Tag(factPath), new { decision = "Accepted", notes = "Checked fixture." }));
        draft.ResourceRefs.FactIds = [factId];
        draft.Claims = s.Claims;
        draft.TechnologyProfile = draft.TechnologyProfile with { SourceRevisionIds = s.TechnologyProfile.SourceRevisionIds,
            FactLocators = new() { [factId] = "Fixture table" } };
        var stale = await Tag(article);
        var saved = await Success(await Write(article + "/draft", stale, draft, HttpMethod.Put));
        var id2 = saved.GetProperty("currentDraftVersionId").GetGuid();
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Write(article + "/draft", stale, draft, HttpMethod.Put)).StatusCode);
        async Task Check(string category, string limitation, Guid revision)
        {
            var detail = await fx.Guest.GetFromJsonAsync<JsonElement>(pub);
            Assert.Equal(category, detail.GetProperty("categorySlug").GetString());
            Assert.Equal(limitation, detail.GetProperty("technologyProfile").GetProperty("limitations").GetString());
            Assert.Contains(detail.GetProperty("sources").EnumerateArray(), c => c.GetProperty("sourceRevisionId").GetGuid() == revision);
            var factsRead = detail.GetProperty("facts");
            if (category == "robotics-and-ai") Assert.Equal("Earth", factsRead[0].GetProperty("testEnvironment").GetString());
            else Assert.Empty(factsRead.EnumerateArray());
            var page = await fx.Guest.GetFromJsonAsync<PageResult<TechnologyCard>>(Root + "/technologies?category=" + category);
            Assert.Contains(page!.Items, x => x.Slug == "moxie" && x.TestEnvironment == "Mars");
        }
        await Check(s.CategorySlug, s.TechnologyProfile!.Limitations, s.Sources[0].SourceRevisionId);
        await Publish(article, id2);
        await Check(draft.CategorySlug, draft.TechnologyProfile.Limitations, rev2.SourceRevisionId);
        var moved = await fx.Guest.GetFromJsonAsync<CategoryResponse[]>(Root + "/categories");
        Assert.Equal(0, moved!.Single(c => c.Slug == s.CategorySlug).PublishedArticleCount);
        Assert.Equal(2, moved!.Single(c => c.Slug == draft.CategorySlug).PublishedArticleCount);
        await Success(await Write(article + "/restore", await Tag(article), new { sourceVersionId = v1.Id, changeSummary = "Restore original evidence" }));
        await Check(draft.CategorySlug, draft.TechnologyProfile.Limitations, rev2.SourceRevisionId);
        await Success(await Write(Sources + rev2.SourceRevisionId + "/revoke", await Tag(Sources + rev2.SourceRevisionId), new { reason = "Fixture withdrawal" }));
        Assert.Equal(HttpStatusCode.NotFound, (await fx.Guest.GetAsync(pub)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fx.Guest.GetAsync($"{Root}/sources/{rev2.SourceId}/revisions/{rev2.SourceRevisionId}")).StatusCode);
        var restored = (await Success(await fx.Admin.GetAsync(article))).GetProperty("currentDraftVersionId").GetGuid();
        await Publish(article, restored);
        await Check(s.CategorySlug, s.TechnologyProfile.Limitations, s.Sources[0].SourceRevisionId);
        await Success(await Write(article + "/unpublish", await Tag(article), new { reason = "Fixture unpublish" }));
        Assert.Equal(HttpStatusCode.NotFound, (await fx.Guest.GetAsync(pub)).StatusCode);
    }
}
