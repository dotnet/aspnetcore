file static class LocalizationHelpers
{
    public static global::Microsoft.Extensions.Localization.IStringLocalizer CreateStringLocalizer(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.Type type,
        global::Microsoft.Extensions.Localization.IStringLocalizerFactory factory)
            => context.ValidationOptions.LocalizerProvider(type, factory)
                ?? throw new global::System.InvalidOperationException(
                    $"The ValidationOptions.LocalizerProvider delegate returned null for type '{type.FullName}'. The delegate must return a non-null IStringLocalizer instance.");
}
