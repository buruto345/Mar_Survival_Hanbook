using Mars_Survival_Hanbook.Contracts;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars_Survival_Hanbook.Services;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SurvivalKnowledgeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is PageQuery query && !context.HttpContext.Request.Query.ContainsKey("language"))
                query.Language = "en";
            if (argument is ArticleDraftRequest draft && !draft.HasExplicitLanguage)
                draft.LanguageCode = "en";
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class LivingOnMarsAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is PageQuery q) q.Language = "en";
            if (argument is ArticleDraftRequest draft)
            {
                if (!draft.HasExplicitLanguage) draft.LanguageCode = "en";
                if (draft.LanguageCode != "en") throw ApiException.Invalid("languageCode must be en.");
            }
            if (argument is PreviewRequest r && (r.LanguageCode is not (null or "en") || r.Language is not (null or "en")))
                throw ApiException.Invalid("language and languageCode must be en.");
        }
    }
}

public sealed record ContentModule(string Slug)
{
    public const string Living = "living-on-mars";
    public const string Discovery = "discover-mars";
    public const string Survival = "survival-knowledge";

    public static ContentModule FromEndpoint(IServiceProvider services)
    {
        var endpoint = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<LivingOnMarsAttribute>() != null) return new(Living);
        return new(endpoint?.Metadata.GetMetadata<SurvivalKnowledgeAttribute>() == null ? Discovery : Survival);
    }
}
