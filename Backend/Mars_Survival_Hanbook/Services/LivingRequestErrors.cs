using Mars_Survival_Hanbook.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mars_Survival_Hanbook.Services;

internal static class LivingRequestErrors
{
    // ModelState may include untrusted JSON property names and localized conversion exceptions.
    private static readonly Dictionary<string, string> Fields = new[] { typeof(PreviewRequest), typeof(ArticleDraftRequest),
        typeof(DiscoveryQuery), typeof(ReviewDecision), typeof(RestoreRequest), typeof(InitialStock) }
        .SelectMany(type => type.GetProperties()).Select(p => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(p.Name))
        .Distinct().ToDictionary(key => key, StringComparer.OrdinalIgnoreCase);

    public static ValidationProblemDetails From(ModelStateDictionary state)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var item in state.Where(s => s.Value?.Errors.Count > 0))
        {
            var parts = item.Key.Split(['.', '[', ']'], StringSplitOptions.RemoveEmptyEntries);
            var name = parts.Select(p => Fields.GetValueOrDefault(p)).FirstOrDefault(field => field != null) ?? "request";
            errors[name] = ["Check the field name, type and allowed range."];
        }
        return new ValidationProblemDetails(errors);
    }
}
