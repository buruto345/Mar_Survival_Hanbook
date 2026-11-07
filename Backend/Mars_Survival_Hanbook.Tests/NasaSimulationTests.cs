using System.Net.Http.Json;
using System.Text.Json;
using Mars_Survival_Hanbook.BackgroundJobs;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class NasaSimulationFactAttribute : FactAttribute
{
    public NasaSimulationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MARS_RUN_SQLSERVER_TESTS") != "1" ||
            Environment.GetEnvironmentVariable("MARS_RUN_NASA_SIMULATION") != "1")
            Skip = "Requires MARS_RUN_SQLSERVER_TESTS=1, MARS_RUN_NASA_SIMULATION=1 and local Postman NASA simulation on port 4010.";
    }
}
public class NasaSimulationTests(DiscoveryHttpFixture fixture) : IClassFixture<DiscoveryHttpFixture>
{
    private async Task Configure()
    {
        await using var db = fixture.Database.CreateContext();
        var c = await db.NasaConnectors.SingleAsync(x => x.Key == "ntrs");
        c.BaseUrl = "http://127.0.0.1:4010/nasa/"; c.RateLimitPerMinute = null;
        await db.SaveChangesAsync();
    }
    private async Task Mode(string mode, string revision = "A")
    {
        using var client = new HttpClient();
        (await client.GetAsync($"http://127.0.0.1:4010/nasa/control?mode={mode}&revision={revision}")).EnsureSuccessStatusCode();
    }
    private async Task<Guid> Queue(string[]? ids = null, string? query = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/discover-mars/imports") {
            Content = JsonContent.Create(new ImportRequest { ConnectorKey = "ntrs", RecordIds = ids ?? [], Query = query, MaxItems = 2, PageSize = 1 }, options: DiscoveryJson.Options)
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await fixture.Admin.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
    }
    private async Task Run()
    {
        using var scope = fixture.Services.CreateScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<IngestionProcessor>().RunOneAsync(default));
    }
    [NasaSimulationFact] public async Task RealWorkerPaginatesDeduplicatesDetectsPdfChangesAndReusesAAfterB()
    {
        await Configure(); await Mode("normal");
        var first = await Queue(query: "Mars"); await Run();
        await using var db = fixture.Database.CreateContext();
        Assert.Equal(IngestionStatus.Succeeded, (await db.IngestionJobs.AsNoTracking().SingleAsync(x => x.Id == first)).Status);
        Assert.Equal(2, await db.IngestionItems.CountAsync(x => x.JobId == first));
        var again = await Queue(["123", "124"]); await Run();
        Assert.Equal(2, (await db.IngestionJobs.AsNoTracking().SingleAsync(x => x.Id == again)).RecordsUnchanged);
        Assert.Equal(2, await db.ScientificSources.CountAsync(x => x.Provider == SourceProvider.Ntrs));
        Assert.Equal(2, await db.SourceRevisions.CountAsync(x => x.Source.Provider == SourceProvider.Ntrs));
        await Mode("pdf-change"); await Queue(["123"]); await Run();
        Assert.Equal(3, await db.SourceRevisions.CountAsync(x => x.Source.Provider == SourceProvider.Ntrs));
        await Mode("normal", "B"); await Queue(["123"]); await Run();
        await Mode("normal", "A"); var back = await Queue(["123"]); await Run();
        Assert.Equal(4, await db.SourceRevisions.CountAsync(x => x.Source.Provider == SourceProvider.Ntrs));
        Assert.Equal(1, (await db.IngestionJobs.AsNoTracking().SingleAsync(x => x.Id == back)).RecordsUnchanged);
        Assert.All(await db.SourceRevisions.Where(x => x.Source.Provider == SourceProvider.Ntrs).ToListAsync(), x => Assert.Equal(VerificationStatus.Pending, x.VerificationStatus));
        using var client = new HttpClient();
        var requests = await client.GetFromJsonAsync<JsonElement>("http://127.0.0.1:4010/nasa/requests");
        Assert.Contains(requests.EnumerateArray(), x => x.GetProperty("path").GetString() == "/api/citations/123");
    }
    [NasaSimulationFact] public async Task TimeoutsRateLimitsServerErrorsAndBadSchemaArePersistedWithoutBreakingPublicReads()
    {
        await Configure();
        foreach (var mode in new[] { "timeout", "429", "500", "invalid" })
        {
            await Mode(mode); var id = await Queue(["123"]);
            for (var attempt = 0; attempt < (mode == "invalid" ? 1 : 3); attempt++)
            {
                await Run();
                await using var retry = fixture.Database.CreateContext();
                await retry.IngestionJobs.Where(x => x.Id == id && x.Status == IngestionStatus.Queued)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
                await retry.IngestionItems.Where(x => x.JobId == id && x.NextAttemptAt != null)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
            }
            await using var db = fixture.Database.CreateContext();
            Assert.Equal(IngestionStatus.Failed, (await db.IngestionJobs.SingleAsync(x => x.Id == id)).Status);
            Assert.Single(await db.IngestionItems.Where(x => x.JobId == id && x.Status == IngestionItemStatus.Failed).ToListAsync());
            Assert.True((await fixture.Guest.GetAsync("/api/discover-mars/articles")).IsSuccessStatusCode);
        }
        await Mode("normal");
    }
    [NasaSimulationFact] public async Task PartialFailureCanRetryOnlyFailedItem()
    {
        await Configure(); await Mode("partial"); var id = await Queue(["123", "124"]); await Run();
        await using var db = fixture.Database.CreateContext();
        Assert.Equal(IngestionStatus.PartiallySucceeded, (await db.IngestionJobs.SingleAsync(x => x.Id == id)).Status);
        var before = await db.IngestionItems.AsNoTracking().Where(x => x.JobId == id && x.ItemKey == "123").SingleAsync();
        await Mode("normal");
        var get = await fixture.Admin.GetAsync("/api/admin/discover-mars/imports/" + id);
        var retry = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/discover-mars/imports/{id}/retry");
        retry.Headers.TryAddWithoutValidation("If-Match", get.Headers.ETag!.ToString());
        Assert.True((await fixture.Admin.SendAsync(retry)).IsSuccessStatusCode);
        await Run();
        var after = await db.IngestionItems.AsNoTracking().Where(x => x.JobId == id && x.ItemKey == "123").SingleAsync();
        Assert.Equal(before.AttemptCount, after.AttemptCount);
        Assert.Equal(IngestionStatus.Succeeded, (await db.IngestionJobs.AsNoTracking().SingleAsync(x => x.Id == id)).Status);
    }
    [NasaSimulationFact] public async Task ImagesPdsAndTrekStageProvenanceAndKeepUnreviewedSamplesPrivate()
    {
        await Mode("normal");
        await using var db = fixture.Database.CreateContext();
        foreach (var key in new[] { "images", "pds", "mars-trek" })
        {
            var connector = await db.NasaConnectors.SingleAsync(x => x.Key == key);
            connector.BaseUrl = "http://127.0.0.1:4010/nasa/"; connector.RateLimitPerMinute = null;
        }
        await db.SaveChangesAsync();
        foreach (var request in new[] {
            new ImportRequest { ConnectorKey = "images", RecordIds = ["PIA_TEST"] },
            new ImportRequest { ConnectorKey = "pds", RecordIds = ["urn:nasa:pds:mars2020_meda:data_calibrated_env:test_pressure::1.0"] },
            new ImportRequest { ConnectorKey = "mars-trek", LayerKey = "mola-color-shade" }
        })
        {
            var http = new HttpRequestMessage(HttpMethod.Post, "/api/admin/discover-mars/imports") {
                Content = JsonContent.Create(request, options: DiscoveryJson.Options) };
            http.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var response = await fixture.Admin.SendAsync(http);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            await Run();
            var job = await db.IngestionJobs.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.True(job.Status == IngestionStatus.Succeeded, job.ErrorMessage ?? (await db.IngestionItems.Where(x => x.JobId == id).Select(x => x.ErrorCode).FirstOrDefaultAsync()));
        }
        var image = await db.MediaAssets.SingleAsync(x => x.NasaId == "PIA_TEST");
        Assert.Equal("NASA/JPL/USGS", image.Credit); Assert.NotNull(image.FileChecksum);
        Assert.Equal(VerificationStatus.Pending, image.VerificationStatus);
        Assert.True(await db.MapLayers.AnyAsync(x => x.LayerIdentifier == "test-mola" && x.Style == "default"));
        var pds = await db.SourceRevisions.SingleAsync(x => x.Source.Provider == SourceProvider.Pds);
        var metadata = DiscoveryJson.Read<SourceMetadata>(pds.MetadataSnapshot);
        var sample = metadata.ProviderMetadata!.Value.GetProperty("samples").GetProperty("candidates")[0];
        Assert.Equal("pending", sample.GetProperty("quality").GetString());
        Assert.Equal(JsonValueKind.Null, sample.GetProperty("observedAt").ValueKind);
        Assert.Equal(0, await db.Measurements.CountAsync());
    }
    [NasaSimulationFact] public async Task RestartAfterItemCommitDoesNotFetchOrDuplicateCompletedItem()
    {
        await Configure(); await Mode("normal"); var id = await Queue(["123"]); await Run();
        await using var db = fixture.Database.CreateContext();
        var item = await db.IngestionItems.AsNoTracking().SingleAsync(x => x.JobId == id);
        var revisionCount = await db.SourceRevisions.CountAsync();
        await db.IngestionJobs.Where(x => x.Id == id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, IngestionStatus.Running)
            .SetProperty(x => x.CompletedAt, (DateTimeOffset?)null).SetProperty(x => x.LeaseOwner, "terminated-worker")
            .SetProperty(x => x.LeaseExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        await Mode("normal"); await Run();
        Assert.Equal(revisionCount, await db.SourceRevisions.CountAsync());
        Assert.Equal(item.AttemptCount, (await db.IngestionItems.AsNoTracking().SingleAsync(x => x.Id == item.Id)).AttemptCount);
        using var client = new HttpClient();
        var requests = await client.GetFromJsonAsync<JsonElement>("http://127.0.0.1:4010/nasa/requests");
        Assert.Empty(requests.EnumerateArray());
    }
}
