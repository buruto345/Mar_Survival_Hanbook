using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Mars_Survival_Hanbook.Contracts;
using Microsoft.AspNetCore.Mvc;
namespace Mars_Survival_Hanbook.Services;

public sealed class DiscoveryOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        var method = context.ApiDescription.HttpMethod;
        var admin = path.StartsWith("api/admin/", StringComparison.Ordinal);
        operation.OperationId = (context.ApiDescription.ActionDescriptor.RouteValues["controller"] ?? context.MethodInfo.DeclaringType!.Name.Replace("Controller", "")) + "_" + context.MethodInfo.Name;
        operation.Summary = method + " /" + path;
        foreach (var parameter in operation.Parameters.Where(x => x.In == ParameterLocation.Query).ToArray())
        {
            if (parameter.Name is "Skip" or "Search") { operation.Parameters.Remove(parameter); continue; }
            parameter.Name = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(parameter.Name);
            parameter.Schema.Default = parameter.Name switch {
                "page" => new Microsoft.OpenApi.Any.OpenApiInteger(1),
                "pageSize" => new Microsoft.OpenApi.Any.OpenApiInteger(20),
                "language" => new Microsoft.OpenApi.Any.OpenApiString((path.Contains("/human-survival") || path.Contains("/survival-knowledge") || path.Contains("/living-on-mars") || path.Contains("/science-technology")) ? "en" : "vi"),
                "sort" => new Microsoft.OpenApi.Any.OpenApiString("publishedAtDesc"),
                "includePlanned" => new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                _ => parameter.Schema.Default
            };
        }
        if (admin || path == "api/auth/me") operation.Security = [new() {
            [new OpenApiSecurityScheme { Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
        }];
        if (admin && context.ApiDescription.HttpMethod is "PUT" or "POST")
        {
            if (path.Contains("{"))
                operation.Parameters.Add(new() { Name = "If-Match", In = ParameterLocation.Header, Required = true,
                    Description = path.Contains("/articles/") ? "Quoted ETag from GET article, including submit/restore/publish."
                        : "Quoted ETag from GET of the source revision, task, job, connector or working resource being changed.", Schema = new() { Type = "string" } });
            if (path.EndsWith("/imports"))
                operation.Parameters.Add(new() { Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true, Schema = new() { Type = "string", MaxLength = 200 } });
        }
        foreach (var code in new[] { "400", "401", "403", "404", "409", "412", "422", "428", "429", "500" })
            operation.Responses.TryAdd(code, new() {
                Description = "Problem Details; code and traceId identify the failure.",
                Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = new() {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository) } }
            });
        if (path == "api/living-on-mars/simulations/preview")
        {
            operation.RequestBody.Content["application/json"].Example = Swashbuckle.AspNetCore.SwaggerGen.OpenApiAnyFactory.CreateFromJson(
                """{"scenarioKey":"outpost","scenarioVersion":1,"durationSols":1,"facilities":{"solar":1,"habitat":2,"electrolyzer":0}}""");
            var fixture = new PreviewRequest("outpost", 1, DurationSols: 1,
                Facilities: new() { ["solar"] = 1, ["habitat"] = 2, ["electrolyzer"] = 0 });
            var example = Living.LivingSimulationService.Run(Living.LivingScenarioService.Baseline(), fixture);
            foreach (var media in operation.Responses["200"].Content.Values)
                media.Example = OpenApiAnyFactory.CreateFromJson(DiscoveryJson.Write(example));
            operation.Responses["400"].Content["application/problem+json"].Example = Swashbuckle.AspNetCore.SwaggerGen.OpenApiAnyFactory.CreateFromJson(
                """{"status":400,"title":"Invalid value for initialStocks.","code":"invalid_request"}""");
        }
        var contractPath = path.StartsWith("api/living-on-mars") ? path.Replace("living-on-mars", "discover-mars") : path;
        contractPath = contractPath.Replace("human-survival", "discover-mars").Replace("science-technology", "discover-mars").Replace("survival-knowledge", "discover-mars");
        var successType = path is "api/human-survival" or "api/human-survival/overview" ? typeof(HumanOverviewResponse)
            : path == "api/human-survival/categories" ? typeof(HumanCategoryResponse[]) : ResponseType(contractPath, method!);
        successType = path switch
        {
            "api/discover-mars" => typeof(DiscoverModuleResponse),
            "api/discover-mars/categories" => typeof(DiscoverCategoryResponse[]),
            "api/discover-mars/categories/{slug}" => typeof(DiscoverCategoryResponse),
            "api/discover-mars/articles/{articleSlug}/images" => typeof(DiscoverImageResponse[]),
            _ => successType
        };
        if (successType != null)
        {
            var status = method == "POST" && (path.EndsWith("/imports") || path.EndsWith("/retry")) ? "202" :
                path.EndsWith("/draft") && method == "PUT" || method == "POST" &&
                (path == "api/auth/register" || path.EndsWith("/articles") || path.EndsWith("/submit") || path.EndsWith("/restore") ||
                 path.Contains("/content/") && !path.Contains("{") || path.EndsWith("/sources/manual") || path.EndsWith("/amend")) ? "201" : "200";
            operation.Responses.Remove("200");
            var response = new OpenApiResponse { Description = status == "202" ? "Work persisted; poll Location for status." : "Success",
                Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() {
                    Schema = context.SchemaGenerator.GenerateSchema(successType, context.SchemaRepository) } } };
            if (admin && (method != "GET" || path.EndsWith("}")) && !path.EndsWith("/health")) response.Headers["ETag"] = new() { Description = "Quoted row version for the returned resource.", Schema = new() { Type = "string" } };
            if (status is "201" or "202") response.Headers["Location"] = new() { Schema = new() { Type = "string" } };
            operation.Responses[status] = response;
            if (path.EndsWith("/sources/manual") || path.EndsWith("/amend")) operation.Responses["200"] = response;
        }
        if (path.EndsWith("/raw") || path.EndsWith("/file"))
            operation.Responses["200"] = new() { Description = "Immutable raw manifest, or a manifest-listed blob. Never an arbitrary filesystem path.",
                Content = new Dictionary<string, OpenApiMediaType> { ["application/octet-stream"] = new() { Schema = new() { Type = "string", Format = "binary" } } } };
        if (path is "api/discover-mars/categories/{slug}/image" or "api/discover-mars/categories/{slug}/images/{nasaId}")
            operation.Responses["200"] = new() { Description = "Category image", Content = new Dictionary<string, OpenApiMediaType> {
                ["image/jpeg"] = new() { Schema = new() { Type = "string", Format = "binary" } } } };
        if (admin)
        {
            var policies = context.MethodInfo.DeclaringType!.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
                .Concat(context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>()).Select(x => x.Policy).Distinct();
            operation.Description = "Requires a current active account. Policies: " + string.Join(", ", policies) +
                ". All CMS policies require Admin. Admin can edit, review (including own content), and publish. " +
                "Viewer has no CMS access. Content validation, workflow states and concurrency checks still apply.";
        }
        if (path.Contains("/science-technology/") && (path.EndsWith("/draft") || method == "POST" && path.EndsWith("/articles")))
            operation.Description += " English only (defaults to en). Supply technologyProfile and one catalog category. Sections with anchors problem and principle, a cited claim and technical source revision IDs are required for review. Qualitative profiles can omit facts. Each selected fact needs a factLocators entry.";
        if ((path.Contains("/survival-knowledge/") || path.Contains("/human-survival/")) && (path.EndsWith("/draft") || method == "POST" && path.EndsWith("/articles")))
            operation.Description += " The only supported languageCode is en. Omitting it selects English.";
        if (path == "api/auth/register") operation.Description = "Create an active Viewer account using username, fullName, email and password. " +
            "Username is 3-50 ASCII letters/digits/dots/underscores/hyphens, starting with a letter or digit; fullName is required (up to 150 characters); " +
            "password is 12-128 characters. Username and email are unique without case distinctions. " +
            "Role and other unknown fields are rejected. Returns 201 with profile; use login for a token. " +
            "Invalid input: 400; duplicate username/email: 409; more than 5 attempts per IP per minute: 429.";
        if (path == "api/auth/login") operation.Description = "Login with email and password. Returns a JWT and profile with username, fullName, email and Admin/Viewer role. Invalid/inactive accounts return 401.";
        if (path == "api/auth/me") operation.Description = "Returns the current active account profile for either Admin or Viewer. Requires a valid Bearer token.";
    }
    private static Type Page(Type type) => typeof(PageResult<>).MakeGenericType(type);
    private static readonly Dictionary<string, Type> Resources = new() {
        ["facts"] = typeof(FactResponse), ["measurements"] = typeof(MeasurementResponse), ["missions"] = typeof(MissionResponse),
        ["locations"] = typeof(LocationResponse), ["timeline"] = typeof(TimelineEventResponse),
        ["timeline-events"] = typeof(TimelineEventResponse), ["media"] = typeof(MediaResponse), ["map-layers"] = typeof(MapLayerResponse)
    };
    private static Type? ResponseType(string path, string method)
    {
        if (path == "api/auth/login") return typeof(LoginResponse);
        if (path is "api/auth/register" or "api/auth/me") return typeof(UserProfile);
        if (path == "api/discover-mars") return typeof(ModuleResponse);
        if (path.StartsWith("api/discover-mars/"))
        {
            var rest = path["api/discover-mars/".Length..].Split('/');
            if (rest[0] == "technologies") return rest.Length == 1 ? Page(typeof(TechnologyCard)) : typeof(ArticleDetailResponse);
            if (rest[0] == "categories") return rest.Length == 1 ? typeof(CategoryResponse[]) : typeof(CategoryResponse);
            if (rest[0] == "articles") return rest.Length == 1 ? Page(typeof(ArticleCard)) : typeof(ArticleDetailResponse);
            if (rest[0] == "sources") return typeof(Citation);
            if (Resources.TryGetValue(rest[0], out var resource)) return rest.Length == 1 ? Page(resource) : resource;
        }
        if (!path.StartsWith("api/admin/discover-mars/")) return null;
        var parts = path["api/admin/discover-mars/".Length..].Split('/');
        switch (parts[0])
        {
            case "content":
                if (Resources.TryGetValue(parts[1], out var content)) return method == "GET" && parts.Length == 2 ? Page(content) : content;
                break;
            case "articles":
                if (parts.Last() == "submit") return typeof(ReviewResponse);
                if (parts.Last() == "preview") return typeof(ArticleDetailResponse);
                if (parts.Length >= 3 && parts[2] == "versions") return parts.Length == 3 ? Page(typeof(ArticleVersionSummary)) : typeof(ArticleVersionResponse);
                return method == "GET" && parts.Length == 1 ? Page(typeof(ArticleAdminResponse)) : typeof(ArticleAdminResponse);
            case "sources":
                if (parts.Last() == "manual") return typeof(SourceRevisionResponse);
                if (parts.Last() == "revisions") return Page(typeof(SourceRevisionSummary));
                return parts.Length == 1 ? Page(typeof(SourceListResponse)) : typeof(SourceResponse);
            case "source-revisions": return path.EndsWith("/raw") ? null : typeof(SourceRevisionResponse);
            case "reviews": return parts.Length == 1 ? Page(typeof(ReviewResponse)) : typeof(ReviewResponse);
            case "imports":
                if (parts.Last() == "items") return Page(typeof(ImportItemResponse));
                return method == "GET" && parts.Length == 1 ? Page(typeof(ImportResponse)) : typeof(ImportResponse);
            case "connectors": return parts.Length == 1 ? typeof(ConnectorResponse[]) : typeof(ConnectorResponse);
        }
        return null;
    }
}
