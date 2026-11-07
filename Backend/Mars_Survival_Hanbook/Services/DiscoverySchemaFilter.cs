using System.Text.Json.Serialization;
using Mars_Survival_Hanbook.Contracts;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace Mars_Survival_Hanbook.Services;

public sealed class DiscoverySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(LoginRequest))
        {
            schema.Description = "Provide password and exactly one non-blank identifier: email or username. Identifiers are trimmed and matched case-insensitively.";
            schema.Required.Add("password");
            schema.Properties["email"].MaxLength = 254;
            schema.Properties["username"].MaxLength = 50;
            schema.Properties["password"].MinLength = 1;
            schema.Properties["password"].MaxLength = 1024;
        }
        if (context.Type == typeof(RegisterRequest))
        {
            schema.AdditionalPropertiesAllowed = false;
            schema.Properties["password"].Format = "password";
            schema.Properties["password"].WriteOnly = true;
        }
        foreach (var p in context.Type.GetProperties().Where(x => x.IsDefined(typeof(JsonRequiredAttribute), true)))
            schema.Required.Add(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(p.Name));
        if (context.Type == typeof(ArticleSnapshot) || context.Type == typeof(ArticleDetailResponse))
        {
            foreach (var (property, type) in new (string, Type)[] {
                ("facts", typeof(FactResponse)), ("measurements", typeof(MeasurementResponse)),
                ("missions", typeof(MissionResponse)), ("locations", typeof(LocationResponse)),
                ("timelineEvents", typeof(TimelineEventResponse)), ("media", typeof(MediaResponse)), ("mapLayers", typeof(MapLayerResponse)) })
                schema.Properties[property] = new() { Type = "array", Items = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository) };
            if (context.Type == typeof(ArticleDetailResponse)) schema.Properties.Remove("relatedArticleIds");
        }
    }
}
