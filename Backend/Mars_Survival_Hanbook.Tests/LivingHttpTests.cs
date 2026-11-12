using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class LivingHttpTests(DiscoveryHttpFixture fixture) : IClassFixture<DiscoveryHttpFixture>
{
    private const string Root = "/api/living-on-mars";

    [SqlServerFact] public async Task PublishReadPreviewAndRevokeFromTheNormalHost()
    {
        await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            DiscoveryCommands.RunAsync(["publish-living-scenario"], fixture.Services)));
        using var db = fixture.Database.CreateContext();
        Assert.Equal(6, await db.Categories.CountAsync(c => c.Module.Slug == "living-on-mars"));
        Assert.Equal(2, await db.LivingScenarios.CountAsync());
        using var req = new HttpRequestMessage(HttpMethod.Post, Root + "/simulations/preview?language=en") {
            Content = JsonContent.Create(LivingSimulationTests.WaterFixture) };
        req.Headers.AcceptLanguage.ParseAdd("vi-VN");
        using var response = await fixture.Guest.SendAsync(req);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Contains("en", response.Content.Headers.ContentLanguage);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(.5m, json.GetProperty("firstShortage").GetProperty("need").GetProperty("unmet").GetDecimal());
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Guest.GetAsync(Root + "/articles?language=vi")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Guest.PostAsJsonAsync(Root + "/simulations/preview",
            LivingSimulationTests.WaterFixture with { LanguageCode = "vi" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Guest.GetAsync("/api/admin/living-on-mars/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Viewer.GetAsync("/api/admin/living-on-mars/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Admin.GetAsync("/api/admin/living-on-mars/articles/" + fixture.Database.ArticleId)).StatusCode);
        var guide = await fixture.Guest.GetFromJsonAsync<JsonElement>(Root + "/articles/outpost-water-and-oxygen");
        Assert.Equal("Choosing which machine runs first", guide.GetProperty("title").GetString());
        var revisionId = (await fixture.Guest.GetFromJsonAsync<ScenarioPack>(Root + "/scenarios/outpost/versions/2"))!.Parameters["oxygen"].SourceRevisionId;
        var rev = await db.SourceRevisions.SingleAsync(r => r.Id == revisionId);
        using var revoke = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/discover-mars/source-revisions/{rev.Id}/revoke") {
            Content = JsonContent.Create(new { reason = "Fixture evidence withdrawn." })
        };
        revoke.Headers.TryAddWithoutValidation("If-Match", Mars_Survival_Hanbook.Services.Concurrency.ETag(rev));
        (await fixture.Admin.SendAsync(revoke)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Guest.GetAsync(Root + "/scenarios/outpost/versions/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Guest.GetAsync(Root + "/articles/outpost-water-and-oxygen")).StatusCode);
        var list = await fixture.Guest.GetFromJsonAsync<JsonElement>(Root + "/scenarios");
        Assert.Equal(1, list[0].GetProperty("version").GetInt32());
    }

    [SqlServerFact] public async Task UnreviewedDraftTextAndLinkedSourceStayPrivate()
    {
        const string admin = "/api/admin/living-on-mars/articles";
        var response = await fixture.Admin.PostAsJsonAsync(admin, new {
            categorySlug = "food", slug = "mixed-guide", title = "Meal plan", summary = "Nội dung chưa duyệt",
            sections = new[] { new { anchor = "meals", heading = "Meals", body = "Nội dung chưa duyệt" } }, claims = Array.Empty<object>() });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.GetProperty("id").GetGuid();
        var version = doc.RootElement.GetProperty("currentDraftVersionId").GetGuid();
        foreach (var url in new[] { admin, admin + "/" + id, $"{admin}/{id}/versions/{version}/preview" })
        {
            var data = await fixture.Admin.GetFromJsonAsync<JsonElement>(url);
            var decoded = System.Text.RegularExpressions.Regex.Unescape(data.GetRawText());
            Assert.DoesNotContain("Nội dung", decoded);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Guest.GetAsync(Root + "/articles/mixed-guide")).StatusCode);
        using var restore = new HttpRequestMessage(HttpMethod.Post, $"{admin}/{id}/restore") {
            Content = JsonContent.Create(new { sourceVersionId = version, changeSummary = "Retry mixed draft" })
        };
        restore.Headers.IfMatch.Add(response.Headers.ETag!);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await fixture.Admin.SendAsync(restore)).StatusCode);
        using var db = fixture.Database.CreateContext();
        var foreignLanguage = db.Articles.Add(new() { CategoryId = await db.Categories.Where(c =>
            c.Module.Slug == "living-on-mars" && c.Slug == "food").Select(c => c.Id).SingleAsync(),
            Slug = "vietnamese-draft", Title = "Bản nháp", Summary = "Nội dung", Content = "", LanguageCode = "vi", CreatedById = fixture.AdminId }).Entity;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Admin.GetAsync(admin + "/" + foreignLanguage.Id)).StatusCode);
        var filtered = await fixture.Admin.GetFromJsonAsync<JsonElement>(admin + "?q=vietnamese-draft");
        Assert.Equal(0, filtered.GetProperty("total").GetInt32());
    }

    [SqlServerFact] public async Task SourceTitleRequiresAnEnglishDisplayBeforeSavingDraft()
    {
        const string originalTitle = "Nguồn tham khảo";
        using var source = await fixture.Admin.PostAsJsonAsync("/api/admin/discover-mars/sources/manual",
            new ManualSourceRequest("https://science.nasa.gov/mars/facts/?test=" + Guid.NewGuid(), originalTitle,
                [new("Fixture author", "Fixture organization")], "Fixture organization",
                Models.Enums.SourceType.WebPage, "Fixture evidence", "Fixture use only", DateTimeOffset.UtcNow), DiscoveryJson.Options);
        source.EnsureSuccessStatusCode();
        var revision = (await source.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var draft = new ArticleDraftRequest { CategorySlug = "food", Slug = "source-display-test", Title = "Food",
            Summary = "Stored rations", LanguageCode = "en", Sections = [new("rations", "Rations", "Track edible mass.")],
            SourceRevisionIds = [revision] };
        const string admin = "/api/admin/living-on-mars/articles";
        using var invalid = await fixture.Admin.PostAsJsonAsync(admin, draft, DiscoveryJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var error = await invalid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("English display text is required for each linked source revision.", error.GetProperty("title").GetString());
        draft.EnglishCitations = new() { [revision] = new("Reference source", null, "Fixture use only") };
        using var saved = await fixture.Admin.PostAsJsonAsync(admin, draft, DiscoveryJson.Options);
        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
        var savedId = (await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currentDraftVersionId").GetGuid();
        await using var db = fixture.Database.CreateContext();
        var frozen = DiscoveryJson.Read<ArticleSnapshot>((await db.ArticleVersions.SingleAsync(v => v.Id == savedId)).Snapshot);
        Assert.Equal("Reference source", Assert.Single(frozen.Sources).Title);
        Assert.Equal(originalTitle, DiscoveryJson.Read<SourceMetadata>((await db.SourceRevisions.SingleAsync(r => r.Id == revision)).MetadataSnapshot).Title);
    }
}
