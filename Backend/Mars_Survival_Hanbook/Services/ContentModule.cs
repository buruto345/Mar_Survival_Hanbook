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

public sealed record ContentModule(string Slug)
{
    public const string Discovery = "discover-mars";
    public const string Survival = "survival-knowledge";

    public static ContentModule FromEndpoint(IServiceProvider services)
    {
        var endpoint = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.GetEndpoint();
        return new(endpoint?.Metadata.GetMetadata<SurvivalKnowledgeAttribute>() == null ? Discovery : Survival);
    }
}
