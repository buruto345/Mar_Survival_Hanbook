using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class DiscoveryHttpTests(DiscoveryHttpFixture f) : IClassFixture<DiscoveryHttpFixture>
{
    private const string Root = "/api/admin/discover-mars";
    private static async Task<(JsonElement Body, string ETag)> Read(HttpResponseMessage r, HttpStatusCode status = HttpStatusCode.OK)
    {
        var text = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == status, $"{r.StatusCode}: {text}");
        return (JsonDocument.Parse(text).RootElement.Clone(), r.Headers.ETag?.ToString() ?? "");
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, object? body = null, string? etag = null, string? key = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body, options: DiscoveryJson.Options);
        if (etag != null) request.Headers.TryAddWithoutValidation("If-Match", etag);
        if (key != null) request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
    private async Task<(Guid Id, Guid SourceId)> Source()
    {
        var result = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/sources/manual",
            new ManualSourceRequest("https://science.nasa.gov/mars/facts/?test=" + Guid.NewGuid(), "Synthetic Mars test source",
                [new("Test author", "NASA test fixture")], "NASA test fixture", SourceType.WebPage,
                "Synthetic evidence used exclusively for software tests.", "Test fixture only.", DateTimeOffset.UtcNow)), HttpStatusCode.Created);
        var id = result.Body.GetProperty("id").GetGuid();
        var approved = await Read(await Send(f.OtherAdmin, HttpMethod.Post, Root + "/source-revisions/" + id + "/decision",
            new SourceDecision(VerificationStatus.Accepted, "Synthetic test decision; no scientific approval claimed."), result.ETag));
        return (id, approved.Body.GetProperty("sourceId").GetGuid());
    }
    private static ArticleDraftRequest Draft(Guid source, string slug) => new() {
        Slug = slug, CategorySlug = "atmosphere-temperature-gravity", Title = "Published synthetic title", Summary = "Synthetic summary",
        Sections = [new("overview", "Overview", "Synthetic plain text content")],
        Claims = [new("claim", "Synthetic claim", EvidenceType.Modeled, TestEnvironment.Simulation, [source], new() { ["section"] = "test" })],
        SourceRevisionIds = [source]
    };
    private async Task<(JsonElement Body, string ETag)> Approve(Guid articleId, Guid versionId, string articleTag)
    {
        var task = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{articleId}/versions/{versionId}/submit", etag: articleTag), HttpStatusCode.Created);
        var taskId = task.Body.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(f.Viewer, HttpMethod.Post, $"{Root}/reviews/{taskId}/claim", etag: task.ETag)).StatusCode);
        var decision = new ReviewDecision(ReviewState.Approved, "Synthetic checklist review.", new(true, true, true, true, true));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(f.Admin, HttpMethod.Post, $"{Root}/reviews/{taskId}/decision", decision, task.ETag)).StatusCode);
        var claimed = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/reviews/{taskId}/claim", etag: task.ETag));
        await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/reviews/{taskId}/decision", decision, claimed.ETag));
        return await Read(await f.Admin.GetAsync($"{Root}/articles/{articleId}"));
    }
    [SqlServerFact] public async Task ModulesIsolateEditorialIdsAndShareOnlyPublishedRevisions()
    {
        const string survival = "/api/admin/survival-knowledge";
        var source = await Source();
        foreach (var (root, other, category) in new[] {
            (survival, Root, "dust-storms"), (Root, survival, "exploration-history") })
        {
            var publicRoot = root.Replace("/admin", "");
            var slug = "scope-" + Guid.NewGuid().ToString("N");
            var draft = Draft(source.Id, slug);
            draft.CategorySlug = category;
            var payload = JsonSerializer.SerializeToNode(draft, DiscoveryJson.Options)!.AsObject();
            payload.Remove("languageCode");
            var created = await Send(f.Admin, HttpMethod.Post, root + "/articles", payload);
            Assert.Contains(root, created.Headers.Location!.ToString());
            var (a, tag) = await Read(created, HttpStatusCode.Created);
            Assert.Equal(root == survival ? "en" : "vi", a.GetProperty("languageCode").GetString());
            var id = a.GetProperty("id").GetGuid();
            var version = a.GetProperty("currentDraftVersionId").GetGuid();
            var otherArticle = $"{other}/articles/{id}";
            Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync(otherArticle)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync($"{otherArticle}/versions/{version}/preview")).StatusCode);
            var submitted = await Send(f.Admin, HttpMethod.Post, $"{root}/articles/{id}/versions/{version}/submit", etag: tag);
            Assert.Contains(root + "/reviews/", submitted.Headers.Location!.ToString());
            var (task, reviewTag) = await Read(submitted, HttpStatusCode.Created);
            var review = $"/reviews/{task.GetProperty("id").GetGuid()}";
            var decision = new ReviewDecision(ReviewState.Approved, "Software fixture", new(true,true,true,true,true));
            Assert.Equal(0, (await Read(await f.Admin.GetAsync($"{other}/reviews?articleId={id}"))).Body.GetProperty("total").GetInt32());
            Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync(other + review)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(f.Admin, HttpMethod.Post, other + review + "/claim", etag: reviewTag)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(f.Admin, HttpMethod.Post, other + review + "/decision", decision, reviewTag)).StatusCode);
            var claimed = await Read(await Send(f.Admin, HttpMethod.Post, root + review + "/claim", etag: reviewTag));
            await Read(await Send(f.Admin, HttpMethod.Post, root + review + "/decision", decision, claimed.ETag));
            var current = await Read(await f.Admin.GetAsync($"{root}/articles/{id}"));
            await Read(await Send(f.Admin, HttpMethod.Post, $"{root}/articles/{id}/publish", new VersionRequest(version), current.ETag));
            var live = await Read(await f.Guest.GetAsync(publicRoot + "/articles/" + slug));
            Assert.Equal(publicRoot.Split('/').Last(), live.Body.GetProperty("moduleSlug").GetString());
            Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync(other.Replace("/admin", "") + "/articles/" + slug)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync(publicRoot + "/articles/" + slug + (root == survival ? "?language=vi" : "?language=en"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await f.Guest.GetAsync($"{publicRoot}/sources/{source.SourceId}/revisions/{source.Id}")).StatusCode);
        }
        var revision = await Read(await f.Admin.GetAsync($"{Root}/source-revisions/{source.Id}"));
        await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/source-revisions/{source.Id}/revoke",
            new ReasonRequest("Cross-module withdrawal test"), revision.ETag));
        foreach (var module in new[] { "survival-knowledge", "discover-mars" })
            Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync($"/api/{module}/sources/{source.SourceId}/revisions/{source.Id}")).StatusCode);
    }
    [SqlServerFact] public async Task SurvivalFactReviewAndConcurrentDraftSavePreserveOneCurrentVersion()
    {
        const string root = "/api/admin/survival-knowledge/articles";
        var source = await Source();
        await using var db = f.Database.CreateContext();
        var unit = await db.Units.SingleAsync(x => x.Symbol == "Pa");
        var fact = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/content/facts",
            new FactRequest { Name = "Test pressure", Value = 600, OriginalValue = 6, OriginalUnit = "mb",
                UnitId = unit.Id, Context = "Test fixture", MeasurementKind = MeasurementKind.Estimate,
                SourceRevisionId = source.Id, EvidenceType = EvidenceType.Observed, TestEnvironment = TestEnvironment.Mars }),
            HttpStatusCode.Created);
        var slug = "test-fact-" + Guid.NewGuid().ToString("N");
        var draft = Draft(source.Id, slug);
        draft.CategorySlug = "oxygen-and-low-pressure";
        draft.LanguageCode = "vi";
        var rejected = await Send(f.Admin, HttpMethod.Post, root, draft);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Contains("Survival Knowledge drafts require languageCode en.", await rejected.Content.ReadAsStringAsync());
        draft.LanguageCode = "en";
        draft.SourceRevisionIds = [Guid.NewGuid()];
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(f.Admin, HttpMethod.Post, root, draft)).StatusCode);
        Assert.False(await db.Articles.AnyAsync(x => x.Slug == slug));
        draft.SourceRevisionIds = [source.Id];
        draft.ResourceRefs.FactIds = [fact.Body.GetProperty("id").GetGuid()];
        var created = await Read(await Send(f.Admin, HttpMethod.Post, root, draft), HttpStatusCode.Created);
        var path = $"{root}/{created.Body.GetProperty("id")}";
        var v = created.Body.GetProperty("currentDraftVersionId").GetGuid();
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await Send(f.Admin, HttpMethod.Post, $"{path}/versions/{v}/submit", etag: created.ETag)).StatusCode);
        var decision = await Read(await Send(f.Admin, HttpMethod.Post,
            $"{Root}/content/facts/{draft.ResourceRefs.FactIds[0]}/decision",
            new SourceDecision(VerificationStatus.Accepted, "Test reviewed value"), fact.ETag));
        Assert.Equal("Accepted", decision.Body.GetProperty("verificationStatus").GetString());
        var competing = await Task.WhenAll(Send(f.Admin, HttpMethod.Put, path + "/draft", draft, created.ETag),
            Send(f.OtherAdmin, HttpMethod.Put, path + "/draft", draft, created.ETag));
        Assert.Single(competing, r => r.StatusCode == HttpStatusCode.PreconditionFailed);
        var saved = await Read(Assert.Single(competing, r => r.StatusCode == HttpStatusCode.Created), HttpStatusCode.Created);
        var next = saved.Body.GetProperty("currentDraftVersionId").GetGuid();
        var task = await Read(await Send(f.Admin, HttpMethod.Post, $"{path}/versions/{next}/submit", etag: saved.ETag), HttpStatusCode.Created);
        var current = await Read(await f.Admin.GetAsync(path));
        await Read(await Send(f.Admin, HttpMethod.Put, path + "/draft", draft, current.ETag), HttpStatusCode.Created);
        var cancelled = await Read(await f.Admin.GetAsync($"/api/admin/survival-knowledge/reviews/{task.Body.GetProperty("id")}"));
        Assert.Equal("Cancelled", cancelled.Body.GetProperty("state").GetString());
    }
    [SqlServerFact] public async Task SurvivalDraftChangesRequireFreshArticleAndTaskEtags()
    {
        const string root = "/api/admin/survival-knowledge";
        const string pub = "/api/survival-knowledge";
        var source = await Source();
        var draft = Draft(source.Id, "english-" + Guid.NewGuid().ToString("N"));
        draft.CategorySlug = "space-radiation";
        draft.LanguageCode = "en";
        draft.Title = "Absorbed dose rate";
        draft.SourceRevisionIds = [source.Id, (await Source()).Id];
        var a = await Read(await Send(f.Admin, HttpMethod.Post, root + "/articles", draft), HttpStatusCode.Created);
        var path = $"{root}/articles/{a.Body.GetProperty("id")}";
        var v1 = a.Body.GetProperty("currentDraftVersionId").GetGuid();
        var submit = $"{path}/versions/{v1}/submit";
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await Send(f.Admin, HttpMethod.Post, submit)).StatusCode);
        var task = await Read(await Send(f.Admin, HttpMethod.Post, submit, etag: a.ETag), HttpStatusCode.Created);
        var current = await Read(await f.Admin.GetAsync(path));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(f.Admin, HttpMethod.Post, submit, etag: current.ETag)).StatusCode);
        var review = $"{root}/reviews/{task.Body.GetProperty("id")}";
        var claimed = await Read(await Send(f.Admin, HttpMethod.Post, review + "/claim", etag: task.ETag));
        var approve = new ReviewDecision(ReviewState.Approved, "Synthetic review.", new(true,true,true,true,true));
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(f.OtherAdmin, HttpMethod.Post, review + "/decision", approve, task.ETag)).StatusCode);
        await Read(await Send(f.Admin, HttpMethod.Post, review + "/decision", approve, claimed.ETag));
        var published = await Read(await Send(f.Admin, HttpMethod.Post, path + "/publish", new VersionRequest(v1), current.ETag));
        draft.CategorySlug = "dust-storms";
        draft.Title = "New draft";
        var edited = await Read(await Send(f.Admin, HttpMethod.Put, path + "/draft", draft, published.ETag), HttpStatusCode.Created);
        var live = await Read(await f.Guest.GetAsync(pub + "/articles/" + draft.Slug));
        Assert.Equal("space-radiation", live.Body.GetProperty("categorySlug").GetString());
        Assert.Equal("Absorbed dose rate", live.Body.GetProperty("title").GetString());
        var v2 = edited.Body.GetProperty("currentDraftVersionId").GetGuid();
        var second = await Read(await Send(f.Admin, HttpMethod.Post, $"{path}/versions/{v2}/submit", etag: edited.ETag), HttpStatusCode.Created);
        var secondPath = $"{root}/reviews/{second.Body.GetProperty("id")}";
        var secondClaim = await Read(await Send(f.Admin, HttpMethod.Post, secondPath + "/claim", etag: second.ETag));
        await Read(await Send(f.Admin, HttpMethod.Post, secondPath + "/decision",
            approve with { Decision = ReviewState.ChangesRequested }, secondClaim.ETag));
        var fresh = await Read(await f.Admin.GetAsync(path));
        var restored = await Read(await Send(f.Admin, HttpMethod.Post, path + "/restore",
            new RestoreRequest(v1, "Restore original"), fresh.ETag), HttpStatusCode.Created);
        var restoredId = restored.Body.GetProperty("currentDraftVersionId").GetGuid();
        Assert.NotEqual(v1, restoredId);
        var preview = await Read(await f.Admin.GetAsync($"{path}/versions/{restoredId}/preview"));
        Assert.Equal("space-radiation", preview.Body.GetProperty("categorySlug").GetString());
        await Read(await Send(f.Admin, HttpMethod.Post, path + "/unpublish", new ReasonRequest("Test complete"), restored.ETag));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync(pub + "/articles/" + draft.Slug)).StatusCode);
    }
    [SqlServerFact] public async Task FullWorkflowFreezesWorkingDataAndRevocationHidesEveryPublicRepresentation()
    {
        var source = await Source();
        await using var db = f.Database.CreateContext();
        var unit = await db.Units.SingleAsync(x => x.Symbol == "Pa");
        var factRequest = new FactRequest { Name = "Synthetic pressure", Value = 600, OriginalValue = 6, OriginalUnit = "hPa",
            UnitId = unit.Id, Context = "Software fixture only", SourceRevisionId = source.Id,
            MeasurementKind = MeasurementKind.Single, EvidenceType = EvidenceType.Modeled, TestEnvironment = TestEnvironment.Simulation };
        var fact = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/content/facts", factRequest), HttpStatusCode.Created);
        var slug = "test-" + Guid.NewGuid().ToString("N");
        var draft = Draft(source.Id, slug); draft.ResourceRefs.FactIds = [fact.Body.GetProperty("id").GetGuid()];
        var article = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = article.Body.GetProperty("id").GetGuid(); var v1 = article.Body.GetProperty("currentDraftVersionId").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync("/api/discover-mars/articles/" + slug)).StatusCode);
        var approved = await Approve(id, v1, article.ETag);
        var publication = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish", new VersionRequest(v1), approved.ETag));
        var before = await Read(await f.Guest.GetAsync("/api/discover-mars/articles/" + slug));
        factRequest.Name = "Changed working fact";
        await Read(await Send(f.Admin, HttpMethod.Put, Root + "/content/facts/" + fact.Body.GetProperty("id").GetGuid(), factRequest, fact.ETag));
        draft.Title = "Private draft title";
        var current = await Read(await f.Admin.GetAsync($"{Root}/articles/{id}"));
        var changed = await Read(await Send(f.Admin, HttpMethod.Put, $"{Root}/articles/{id}/draft", draft, current.ETag), HttpStatusCode.Created);
        var after = await Read(await f.Guest.GetAsync("/api/discover-mars/articles/" + slug));
        Assert.Equal(before.Body.GetRawText(), after.Body.GetRawText());
        var acceptedRevision = await Read(await f.Admin.GetAsync(Root + "/source-revisions/" + source.Id));
        var pendingRevision = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/source-revisions/{source.Id}/amend",
            new SourceAmendmentRequest("Pending updated source", [new("Fixture author", "NASA fixture")], "NASA fixture",
                "Changed metadata requiring a new review.", "Test fixture only.", null), acceptedRevision.ETag), HttpStatusCode.Created);
        Assert.Equal("Pending", pendingRevision.Body.GetProperty("verificationStatus").GetString());
        Assert.Equal(before.Body.GetRawText(), (await Read(await f.Guest.GetAsync("/api/discover-mars/articles/" + slug))).Body.GetRawText());
        Assert.True((await Read(await f.Admin.GetAsync($"{Root}/articles/{id}"))).Body.GetProperty("requiresReview").GetBoolean());
        var search = await Read(await f.Guest.GetAsync("/api/discover-mars/articles?q=Private%20draft%20title"));
        Assert.Equal(0, search.Body.GetProperty("total").GetInt32());
        var facts = await Read(await f.Guest.GetAsync("/api/discover-mars/facts?q=Synthetic%20pressure"));
        Assert.True(facts.Body.GetProperty("total").GetInt32() > 0);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish",
            new VersionRequest(v1), publication.ETag)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish",
            new VersionRequest(changed.Body.GetProperty("currentDraftVersionId").GetGuid()), changed.ETag)).StatusCode);
        var rev = await Read(await f.OtherAdmin.GetAsync(Root + "/source-revisions/" + source.Id));
        await Read(await Send(f.OtherAdmin, HttpMethod.Post, $"{Root}/source-revisions/{source.Id}/revoke",
            new ReasonRequest("Synthetic revocation test."), rev.ETag));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync("/api/discover-mars/articles/" + slug)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync($"/api/discover-mars/sources/{source.SourceId}/revisions/{source.Id}")).StatusCode);
        var hidden = await Read(await f.Guest.GetAsync("/api/discover-mars/facts?q=Synthetic%20pressure"));
        Assert.Equal(0, hidden.Body.GetProperty("total").GetInt32());
    }
    [SqlServerFact] public async Task SeedPaginationAuthAndQueryValidation()
    {
        await using var db = f.Database.CreateContext();
        await DiscoverySeed.RunAsync(db); await DiscoverySeed.RunAsync(db);
        Assert.Equal(1, await db.Modules.CountAsync(x => x.Slug == "discover-mars"));
        Assert.Equal(5, await db.Categories.CountAsync(x => x.Module.Slug == "discover-mars"));
        var categories = await Read(await f.Guest.GetAsync("/api/discover-mars/categories"));
        Assert.Equal(5, categories.Body.GetArrayLength());
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Guest.GetAsync(Root + "/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(f.Viewer, HttpMethod.Post, Root + "/articles/" + Guid.NewGuid() + "/publish", new VersionRequest(Guid.NewGuid()))).StatusCode);
        foreach (var path in new[] { "articles?page=0", "articles?pageSize=101", "locations?bbox=170,0,-170,20",
            "measurements", "timeline?from=2025-02-01&to=2025-01-01", "missions?status=999", "missions?status=1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.Guest.GetAsync("/api/discover-mars/" + path)).StatusCode);
        var empty = await Read(await f.Guest.GetAsync("/api/discover-mars/articles?q=doesnotexist"));
        Assert.Equal(0, empty.Body.GetProperty("total").GetInt32());
    }
    [SqlServerFact] public async Task IdempotencyIsScopedAndChangedBodiesConflict()
    {
        var key = Guid.NewGuid().ToString();
        var request = new ImportRequest { ConnectorKey = "ntrs", RecordIds = ["20240012934"], MaxItems = 20 };
        var first = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/imports", request, key: key), HttpStatusCode.Accepted);
        var again = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/imports", request, key: key), HttpStatusCode.Accepted);
        Assert.Equal(first.Body.GetProperty("id").GetGuid(), again.Body.GetProperty("id").GetGuid());
        request.RecordIds = ["20240012935"];
        Assert.Equal(HttpStatusCode.Conflict, (await Send(f.Admin, HttpMethod.Post, Root + "/imports", request, key: key)).StatusCode);
    }
    [SqlServerFact] public async Task SnapshotAndDependenciesAreImmutable()
    {
        var source = await Source(); var draft = Draft(source.Id, "immutable-" + Guid.NewGuid().ToString("N"));
        var a = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        await using var db = f.Database.CreateContext();
        var v = await db.ArticleVersions.SingleAsync(x => x.Id == a.Body.GetProperty("currentDraftVersionId").GetGuid());
        v.Snapshot = "{}";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var link = await db.ArticleVersionSources.FirstAsync(x => x.ArticleVersionId == v.Id);
        db.ArticleVersionSources.Remove(link);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
    [SqlServerFact] public async Task ApprovalCannotBeReusedForANewerDraftAndClaimsKeepIndependentEvidence()
    {
        var source = await Source();
        var draft = Draft(source.Id, "approval-" + Guid.NewGuid().ToString("N"));
        draft.Claims = [draft.Claims[0], new("observed", "Synthetic observed claim", EvidenceType.Observed,
            TestEnvironment.Mars, [source.Id], new() { ["table"] = "fixture" })];
        var article = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = article.Body.GetProperty("id").GetGuid(); var v1 = article.Body.GetProperty("currentDraftVersionId").GetGuid();
        var approved = await Approve(id, v1, article.ETag);
        draft.Title = "New unapproved draft";
        var newer = await Read(await Send(f.Admin, HttpMethod.Put, $"{Root}/articles/{id}/draft", draft, approved.ETag), HttpStatusCode.Created);
        var v2 = newer.Body.GetProperty("currentDraftVersionId").GetGuid();
        foreach (var version in new[] { v1, v2 })
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(f.Admin, HttpMethod.Post,
                $"{Root}/articles/{id}/publish", new VersionRequest(version), newer.ETag)).StatusCode);
        var preview = await Read(await f.Admin.GetAsync($"{Root}/articles/{id}/versions/{v1}/preview"));
        Assert.Equal("Modeled", preview.Body.GetProperty("claims")[0].GetProperty("evidenceType").GetString());
        Assert.Equal("Observed", preview.Body.GetProperty("claims")[1].GetProperty("evidenceType").GetString());
    }
    [SqlServerFact] public async Task PendingAndRejectedSourcesCannotEnterScientificReview()
    {
        var revision = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/sources/manual",
            new ManualSourceRequest("https://science.nasa.gov/mars/facts/?gate=" + Guid.NewGuid(), "Synthetic pending source",
                [new("Fixture", "NASA fixture")], "NASA fixture", SourceType.WebPage, "Test evidence", "Test only", DateTimeOffset.UtcNow)), HttpStatusCode.Created);
        var revisionId = revision.Body.GetProperty("id").GetGuid();
        var draft = Draft(revisionId, "gate-" + Guid.NewGuid().ToString("N"));
        var article = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = article.Body.GetProperty("id").GetGuid(); var version = article.Body.GetProperty("currentDraftVersionId").GetGuid();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(f.Admin, HttpMethod.Post,
            $"{Root}/articles/{id}/versions/{version}/submit", etag: article.ETag)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(f.Viewer, HttpMethod.Post, $"{Root}/source-revisions/{revisionId}/decision",
            new SourceDecision(VerificationStatus.Accepted, "Viewer cannot approve sources"), revision.ETag)).StatusCode);
        await Read(await Send(f.OtherAdmin, HttpMethod.Post, $"{Root}/source-revisions/{revisionId}/decision",
            new SourceDecision(VerificationStatus.Rejected, "Synthetic rejection"), revision.ETag));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(f.Admin, HttpMethod.Post,
            $"{Root}/articles/{id}/versions/{version}/submit", etag: article.ETag)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync("/api/discover-mars/articles/" + draft.Slug)).StatusCode);
    }
    [SqlServerFact] public async Task PublishedMediaServesFrozenBytesAndRevocationImmediatelyBlocksTheFile()
    {
        var source = await Source();
        using var scope = f.Services.CreateScope();
        var raw = scope.ServiceProvider.GetRequiredService<Mars_Survival_Hanbook.Services.Sources.RawStorage>();
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jfYQAAAAASUVORK5CYII=");
        var stored = await raw.PutAsync(bytes, default, "image/png");
        var rawRoot = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Storage:RawRoot"]!;
        Assert.True(File.Exists(Path.Combine(rawRoot, "ImageMarHanbook", stored.Hash + ".png")));
        Assert.False(File.Exists(Path.Combine(rawRoot, stored.Hash)));
        var media = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/content/media", new MediaRequest {
            SourceRevisionId = source.Id, Title = "Synthetic pixel", MediaType = MediaType.Image,
            Url = "https://images-assets.nasa.gov/test.png", OriginalUrl = "https://images-assets.nasa.gov/test.png",
            Credit = "Synthetic fixture", UsageNote = "Test only", AltText = "Synthetic pixel", FileChecksum = stored.Hash, MimeType = "image/png"
        }), HttpStatusCode.Created);
        var draft = Draft(source.Id, "media-" + Guid.NewGuid().ToString("N"));
        draft.LanguageCode = "en";
        draft.ResourceRefs.MediaIds = [media.Body.GetProperty("id").GetGuid()];
        draft.MediaPlacements = [new(draft.ResourceRefs.MediaIds[0], 10, "Frozen test caption")];
        var article = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = article.Body.GetProperty("id").GetGuid(); var v = article.Body.GetProperty("currentDraftVersionId").GetGuid();
        var approved = await Approve(id, v, article.ETag);
        await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish", new VersionRequest(v), approved.ETag));
        var published = await Read(await f.Guest.GetAsync("/api/discover-mars/articles/" + draft.Slug + "?language=en"));
        var url = published.Body.GetProperty("media")[0].GetProperty("url").GetString()!;
        Assert.Equal("Frozen test caption", published.Body.GetProperty("media")[0].GetProperty("caption").GetString());
        Assert.StartsWith("/api/discover-mars/media/", url);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync(url.Replace("discover-mars", "survival-knowledge"))).StatusCode);
        var legacyUrl = url.Replace("&language=en", "");
        Assert.NotEqual(url, legacyUrl);
        Assert.Equal(HttpStatusCode.OK, (await f.Guest.GetAsync(legacyUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync(legacyUrl + "&language=vi")).StatusCode);
        var image = await f.Guest.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode); Assert.Equal(bytes, await image.Content.ReadAsByteArrayAsync());
        Assert.True(image.Headers.CacheControl!.NoStore);
        var revision = await Read(await f.OtherAdmin.GetAsync(Root + "/source-revisions/" + source.Id));
        await Read(await Send(f.OtherAdmin, HttpMethod.Post, $"{Root}/source-revisions/{source.Id}/revoke",
            new ReasonRequest("Synthetic file revocation test"), revision.ETag));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync(url)).StatusCode);
    }
    [SqlServerFact] public async Task ConcurrentImportsOfOneManualSourceHaveOneIdentityAndFingerprint()
    {
        var url = "https://science.nasa.gov/mars/facts/?race=" + Guid.NewGuid();
        var request = new ManualSourceRequest(url, "Concurrent synthetic source", [new("Fixture", "NASA fixture")],
            "NASA fixture", SourceType.WebPage, "Synthetic evidence.", "Test only.", DateTimeOffset.UtcNow);
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Send(f.Admin, HttpMethod.Post, Root + "/sources/manual", request)));
        foreach (var response in responses) Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        await using var db = f.Database.CreateContext();
        var source = Assert.Single(await db.ScientificSources.Where(x => x.Url == url).ToListAsync());
        Assert.Single(await db.SourceRevisions.Where(x => x.SourceId == source.Id).ToListAsync());
    }
    [SqlServerFact] public async Task CurrentAccountStateAndRoleOverrideExistingToken()
    {
        await using var db = f.Database.CreateContext();
        await db.Users.Where(x => x.Id == f.AdminId).ExecuteUpdateAsync(u => u.SetProperty(x => x.State, UserState.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Admin.GetAsync(Root + "/articles")).StatusCode);
        await db.Users.Where(x => x.Id == f.AdminId).ExecuteUpdateAsync(u => u.SetProperty(x => x.State, UserState.Active).SetProperty(x => x.Role, UserRole.Viewer));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Admin.GetAsync(Root + "/articles")).StatusCode);
        await db.Users.Where(x => x.Id == f.AdminId).ExecuteUpdateAsync(u => u.SetProperty(x => x.Role, UserRole.Admin));
        Assert.True((await f.Admin.GetAsync(Root + "/articles")).IsSuccessStatusCode);
    }
    [SqlServerFact] public async Task PublishAndRevokeRaceCannotExposeInvalidPublication()
    {
        var source = await Source();
        var draft = Draft(source.Id, "race-" + Guid.NewGuid().ToString("N"));
        var a = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = a.Body.GetProperty("id").GetGuid(); var v = a.Body.GetProperty("currentDraftVersionId").GetGuid();
        var approved = await Approve(id, v, a.ETag);
        var r = await Read(await f.OtherAdmin.GetAsync(Root + "/source-revisions/" + source.Id));
        var results = await Task.WhenAll(
            Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish", new VersionRequest(v), approved.ETag),
            Send(f.OtherAdmin, HttpMethod.Post, $"{Root}/source-revisions/{source.Id}/revoke", new ReasonRequest("Concurrent revocation test"), r.ETag));
        Assert.Contains(results[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.PreconditionFailed, HttpStatusCode.UnprocessableEntity });
        Assert.Equal(HttpStatusCode.OK, results[1].StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync("/api/discover-mars/articles/" + draft.Slug)).StatusCode);
    }
    [SqlServerFact] public async Task PublicationTransactionRollsBackWhenAuditInsertFails()
    {
        var source = await Source();
        var draft = Draft(source.Id, "rollback-" + Guid.NewGuid().ToString("N"));
        var a = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = a.Body.GetProperty("id").GetGuid(); var v = a.Body.GetProperty("currentDraftVersionId").GetGuid();
        var approved = await Approve(id, v, a.ETag);
        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER TestRejectPublishAudit ON AuditLogs AFTER INSERT AS
            BEGIN IF EXISTS (SELECT 1 FROM inserted WHERE Action = 'Published') THROW 51001, 'Synthetic audit failure', 1; END
            """);
        try
        {
            Assert.Equal(HttpStatusCode.InternalServerError, (await Send(f.Admin, HttpMethod.Post,
                $"{Root}/articles/{id}/publish", new VersionRequest(v), approved.ETag)).StatusCode);
            Assert.Null((await db.Articles.SingleAsync(x => x.Id == id)).PublishedVersionId);
            Assert.False(await db.AuditLogs.AnyAsync(x => x.EntityId == id && x.Action == AuditAction.Published));
        }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TestRejectPublishAudit"); }
    }
    [SqlServerFact] public async Task ResourcesUsePinnedRepresentationsAndOldVersionsDisappearAfterRepublish()
    {
        var source = await Source();
        async Task<Guid> Create(string kind, object request)
        {
            var result = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/content/" + kind, request), HttpStatusCode.Created);
            return result.Body.GetProperty("id").GetGuid();
        }
        var suffix = Guid.NewGuid().ToString("N");
        var mission = await Create("missions", new MissionRequest { NasaIdentifier = suffix, Name = "Mission " + suffix, Agency = "NASA test",
            SourceRevisionId = source.Id, MissionType = MissionType.Rover, Status = MissionStatus.Active, StatusVerifiedAt = DateTimeOffset.UtcNow });
        var location = await Create("locations", new LocationRequest { Name = "Location " + suffix, Slug = suffix,
            Latitude = 1, Longitude = 2, CoordinateSystem = "Mars planetocentric east -180..180", SourceRevisionId = source.Id });
        var evt = await Create("timeline-events", new TimelineEventRequest { Title = "Event " + suffix, Description = "Test event",
            Agency = "NASA test", EventDate = DateTimeOffset.UtcNow, MissionId = mission, SourceRevisionId = source.Id });
        await using var db = f.Database.CreateContext();
        var unit = await db.Units.SingleAsync(x => x.Symbol == "Pa");
        var fact = await Create("facts", new FactRequest { Name = "Fact " + suffix, Value = 600, OriginalValue = 6,
            OriginalUnit = "hPa", UnitId = unit.Id, Context = "Synthetic fixture", SourceRevisionId = source.Id,
            EvidenceType = EvidenceType.Observed, TestEnvironment = TestEnvironment.Mars, MeasurementKind = MeasurementKind.Single });
        var observed = DateTimeOffset.UtcNow;
        var measurement = await Create("measurements", new MeasurementRequest { FactId = fact, UnitId = unit.Id, Value = 600,
            OriginalValue = 6, OriginalUnit = "hPa", ObservedAt = observed, Context = "Synthetic fixture", Instrument = "Test",
            SourceRevisionId = source.Id, MissionId = mission, LocationId = location, QualityFlag = "valid",
            EvidenceType = EvidenceType.Observed, TestEnvironment = TestEnvironment.Mars, MeasurementKind = MeasurementKind.Single });
        var map = await Create("map-layers", new MapLayerRequest { Name = "Map " + suffix, ServiceUrl = "https://trek.nasa.gov/tiles/",
            LayerIdentifier = suffix, TileMatrixSet = "default", Format = "image/jpeg", Attribution = "NASA test",
            Style = "default", ResourceUrlTemplate = "https://trek.nasa.gov/tiles/{TileMatrix}/{TileRow}/{TileCol}.jpg",
            BoundingBoxJson = "[-180,-90,180,90]", CoordinateSystem = "Mars EQ", LocationId = location, SourceRevisionId = source.Id });
        var draft = Draft(source.Id, "resources-" + suffix);
        draft.ResourceRefs = new() { FactIds = [fact], MeasurementIds = [measurement], MissionIds = [mission], LocationIds = [location],
            TimelineEventIds = [evt], MapLayerIds = [map] };
        var a = await Read(await Send(f.Admin, HttpMethod.Post, Root + "/articles", draft), HttpStatusCode.Created);
        var id = a.Body.GetProperty("id").GetGuid(); var v1 = a.Body.GetProperty("currentDraftVersionId").GetGuid();
        var approved = await Approve(id, v1, a.ETag);
        var published = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish", new VersionRequest(v1), approved.ETag));
        foreach (var path in new[] { $"missions?q={suffix}", $"locations?q={suffix}&bbox=0,0,10,10",
            $"timeline?missionId={mission}", $"map-layers?locationId={location}",
            $"measurements?factId={fact}&from={Uri.EscapeDataString(observed.AddDays(-1).ToString("O"))}&to={Uri.EscapeDataString(observed.AddDays(1).ToString("O"))}" })
        {
            var list = await Read(await f.Guest.GetAsync("/api/discover-mars/" + path));
            Assert.Equal(1, list.Body.GetProperty("total").GetInt32());
        }
        var detail = await Read(await f.Guest.GetAsync($"/api/discover-mars/missions/{mission}?articleVersionId={v1}"));
        Assert.Equal(v1, detail.Body.GetProperty("articleVersionId").GetGuid());
        Assert.Equal(source.Id, detail.Body.GetProperty("source").GetProperty("sourceRevisionId").GetGuid());
        var second = await Read(await Send(f.Admin, HttpMethod.Put, $"{Root}/articles/{id}/draft", draft, published.ETag), HttpStatusCode.Created);
        var v2 = second.Body.GetProperty("currentDraftVersionId").GetGuid();
        approved = await Approve(id, v2, second.ETag);
        published = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/publish", new VersionRequest(v2), approved.ETag));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync($"/api/discover-mars/missions/{mission}?articleVersionId={v1}")).StatusCode);
        var restore = await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/restore",
            new RestoreRequest(v1, "Restore test"), published.ETag), HttpStatusCode.Created);
        Assert.NotEqual(v1, restore.Body.GetProperty("currentDraftVersionId").GetGuid());
        await Read(await Send(f.Admin, HttpMethod.Post, $"{Root}/articles/{id}/unpublish", new ReasonRequest("Test unpublish"), restore.ETag));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Guest.GetAsync($"/api/discover-mars/missions/{mission}?articleVersionId={v2}")).StatusCode);
    }
}
