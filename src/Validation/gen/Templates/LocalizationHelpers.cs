file static class LocalizationHelpers
{
    public static global::Microsoft.Extensions.Localization.IStringLocalizer CreateStringLocalizer(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.Type type,
        global::Microsoft.Extensions.Localization.IStringLocalizerFactory factory)
            => context.ValidationOptions.LocalizerProvider(type, factory)
                ?? throw new global::System.InvalidOperationException(
                    $"The ValidationOptions.LocalizerProvider delegate returned null for type '{type.FullName}'. The delegate must return a non-null IStringLocalizer instance.");

    public static string? FindLocalizedTemplate(
        global::Microsoft.Extensions.Localization.IStringLocalizer localizer,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute,
        string? memberName,
        global::System.Type declaringType)
    {
        if (!string.IsNullOrEmpty(attribute.ErrorMessage))
        {
            var explicitMatch = localizer[attribute.ErrorMessage!];
            if (!explicitMatch.ResourceNotFound)
            {
                return explicitMatch.Value;
            }
        }

        var attributeName = attribute.GetType().Name;
        var typeName = GetKeySegment(declaringType);

        if (memberName is not null)
        {
            var memberMatch = localizer[$"{typeName}_{memberName}_{attributeName}_Error"];
            if (!memberMatch.ResourceNotFound)
            {
                return memberMatch.Value;
            }
        }

        var typeMatch = localizer[$"{typeName}_{attributeName}_Error"];
        if (!typeMatch.ResourceNotFound)
        {
            return typeMatch.Value;
        }

        var globalMatch = localizer[$"{attributeName}_Error"];

        return globalMatch.ResourceNotFound ? null : globalMatch.Value;
    }

    private static string GetKeySegment(global::System.Type type)
    {
        var name = (global::System.Nullable.GetUnderlyingType(type) ?? type).Name;
        var arityIndex = name.IndexOf('`');

        return arityIndex < 0 ? name : name[..arityIndex];
    }
}
