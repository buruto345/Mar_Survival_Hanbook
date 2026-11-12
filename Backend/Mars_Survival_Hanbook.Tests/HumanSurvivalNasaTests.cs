using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars_Survival_Hanbook.Data.HumanSurvival;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

[Collection("NASA simulation mode")]
public sealed class HumanSurvivalNasaTests(DiscoveryHttpFixture fixture) : IClassFixture<DiscoveryHttpFixture>
{
    [NasaSimulationFact]
    public async Task InterruptedNtrsImportReusesCommittedRevisionOnRetry()
    {
        await using var db = fixture.Database.CreateContext();
        var connector = await db.NasaConnectors.SingleAsync(c => c.Key == "ntrs");
        connector.BaseUrl = "http://127.0.0.1:4010/nasa/";
        await db.SaveChangesAsync();
        using var mock = new HttpClient { BaseAddress = new Uri(connector.BaseUrl) };
        var pack = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "HumanSurvival", "release.en.json")))!;
        var sources = pack["sources"]!.AsArray();
        sources[0]!["ntrsId"] = "123";
        sources[1]!["ntrsId"] = "124";
        pack["articles"] = new JsonArray(pack["articles"]!.AsArray().Take(2).Select(a => a!.DeepClone()).ToArray());
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, pack.ToJsonString());
            (await mock.GetAsync("control?mode=partial")).EnsureSuccessStatusCode();
            var failed = await Assert.ThrowsAsync<InvalidOperationException>(() => HumanSurvivalImport.RunAsync(fixture.Services, file));
            Assert.Contains("a-conversation-between-shifts", failed.Message);
            var versions = db.ArticleVersions.Where(v => v.Article.Category.Module.Slug == "human-survival");
            var original = await versions.Select(v => v.Id).SingleAsync();

            (await mock.GetAsync("control?mode=normal")).EnsureSuccessStatusCode();
            await HumanSurvivalImport.RunAsync(fixture.Services, file);
            var requests = await mock.GetFromJsonAsync<JsonElement>("requests");
            Assert.Contains(requests.EnumerateArray(), r => r.GetProperty("path").GetString() == "/api/citations/124");
            Assert.DoesNotContain(requests.EnumerateArray(), r => r.GetProperty("path").GetString() == "/api/citations/123");
            (await mock.GetAsync("control?mode=500")).EnsureSuccessStatusCode();
            await HumanSurvivalImport.RunAsync(fixture.Services, file);
            Assert.Empty((await mock.GetFromJsonAsync<JsonElement>("requests")).EnumerateArray());
            Assert.Equal(2, await versions.CountAsync());
            Assert.True(await versions.AnyAsync(v => v.Id == original));
        }
        finally
        {
            File.Delete(file);
            (await mock.GetAsync("control?mode=normal")).EnsureSuccessStatusCode();
        }
    }
}
