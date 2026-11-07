using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mars_Survival_Hanbook.Services;

public sealed class StrictEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type.IsEnum ? new StrictEnumModelBinder(type) : null;
    }
}
internal sealed class StrictEnumModelBinder(Type type) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var name = value.FirstValue;
        if (name != null && Enum.GetNames(type).Contains(name, StringComparer.OrdinalIgnoreCase))
            context.Result = ModelBindingResult.Success(Enum.Parse(type, name, ignoreCase: true));
        else context.ModelState.TryAddModelError(context.ModelName, "Use a supported enum name; numeric enum values are not accepted.");
        return Task.CompletedTask;
    }
}
