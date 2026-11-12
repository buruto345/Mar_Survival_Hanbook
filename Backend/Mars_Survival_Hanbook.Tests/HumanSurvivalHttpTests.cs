using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data.HumanSurvival;
using Mars_Survival_Hanbook.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class HumanSurvivalHttpTests(DiscoveryHttpFixture fixture) : IClassFixture<DiscoveryHttpFixture>
{
    private const string Root = "/api/human-survival";
    private const string Admin = "/api/admin/human-survival/articles";

    [SqlServerFact]
    public async Task RetryImportAndWithdrawRequiredEvidence()
    {
        var original = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "HumanSurvival", "release.en.json"));
        var pack = JsonNode.Parse(original)!.AsObject();
        // Exercise real SQL persistence offline using the edition's two manual sources.
        var entries = pack["articles"]!.AsArray().Where(e => e!["source"]!.ToString() is "food" or "hera")
            .Select(e => e!.DeepClone()).ToArray();
        pack["articles"] = new JsonArray(entries);
        var file = Path.GetTempFileName();
        async Task Import()
        {
            await File.WriteAllTextAsync(file, pack.ToJsonString());
            await HumanSurvivalImport.RunAsync(fixture.Services, file);
        }
        async Task<JsonElement> Change(string path, string action, object body)
        {
            using var get = await fixture.Admin.GetAsync(path);
            using var request = new HttpRequestMessage(HttpMethod.Post, path + action) { Content = JsonContent.Create(body) };
            request.Headers.IfMatch.Add(get.Headers.ETag!);
            using var response = await fixture.Admin.SendAsync(request);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        try
        {
            pack["languageCode"] = "vi";
            await Assert.ThrowsAsync<ApiException>(Import);
            pack["languageCode"] = "en";
            entries[1]["source"] = "missing";
            var error = await Assert.ThrowsAsync<InvalidOperationException>(Import);
            Assert.Contains("sharing-the-common-room", error.Message);
            entries[1]["source"] = "hera";
            await Import();
            await Import();
            using var db = fixture.Database.CreateContext();
            var versions = db.ArticleVersions.Where(v => v.Article.Category.Module.Slug == ContentModule.Human);
            Assert.Equal(2, await versions.CountAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Guest.GetAsync(Root + "/articles?language=vi")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Viewer.GetAsync(Admin)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.Admin.GetAsync(Admin + "/" + fixture.Database.ArticleId)).StatusCode);

            var initial = await fixture.Guest.GetFromJsonAsync<HumanOverviewResponse>(Root + "/overview");
            Assert.Equal(6, initial!.Categories.Length);
            Assert.All(initial.Categories, c => Assert.Null(c.ArticleSlug));
            var version = await versions.AsNoTracking().SingleAsync(v => v.CategorySlug == "nutrition");
            var revision = await db.ArticleVersionSources.SingleAsync(s => s.ArticleVersionId == version.Id);
            var source = "/api/admin/discover-mars/source-revisions/" + revision.SourceRevisionId;
            await Change(source, "/decision", new { decision = "Accepted", notes = "Verified meal preparation locator." });
            var path = Admin + "/" + version.ArticleId;
            var task = await Change(path, $"/versions/{version.Id}/submit", new { });
            var review = "/api/admin/human-survival/reviews/" + task.GetProperty("id").GetGuid();
            await Change(review, "/claim", new { });
            await Change(review, "/decision", new { decision = "Approved", notes = "Reviewed English snapshot and source.",
                checklist = new ReviewChecklist(true, true, true, true, true) });
            await Change(path, "/publish", new { versionId = version.Id });
            entries[0]["summary"] = "A revised meal plan.";
            await Import();
            Assert.Equal(3, await versions.CountAsync());
            var detail = Root + "/articles/dinner-on-the-late-shift";
            var published = await fixture.Guest.GetFromJsonAsync<JsonElement>(detail);
            Assert.Equal(version.Id, published.GetProperty("articleVersionId").GetGuid());
            await Change(source, "/revoke", new { reason = "Withdraw source during acceptance check." });
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.Guest.GetAsync(detail)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.Guest.GetAsync(
                $"{Root}/sources/{revision.SourceId}/revisions/{revision.SourceRevisionId}")).StatusCode);
            var cards = await fixture.Guest.GetFromJsonAsync<PageResult<ArticleCard>>(Root + "/articles");
            Assert.Empty(cards!.Items);
            var after = await fixture.Guest.GetFromJsonAsync<HumanOverviewResponse>(Root + "/overview");
            Assert.Equal(0, after!.PublishedArticleCount);
            Assert.All(after.Categories, c => Assert.Null(c.ArticleSlug));
        }
        finally { File.Delete(file); }
    }
}
