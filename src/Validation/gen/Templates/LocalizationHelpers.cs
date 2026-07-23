file static class LocalizationHelpers
{
    public static global::Microsoft.Extensions.Localization.IStringLocalizer CreateStringLocalizer(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.Type? type,
        global::Microsoft.Extensions.Localization.IStringLocalizerFactory factory)
        => context.ValidationOptions.LocalizerProvider is { } provider
            ? provider(type, factory)
                ?? throw new global::System.InvalidOperationException(
                    $"The ValidationOptions.LocalizerProvider delegate returned null for type '{type?.FullName ?? "<null>"}'. The delegate must return a non-null IStringLocalizer instance.")
            : factory.Create(type ?? typeof(object));
}
