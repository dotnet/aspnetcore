// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization.Attributes;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Configures <see cref="ValidationOptions"/> based on
/// <see cref="ValidationLocalizationOptions"/> and the registered
/// <see cref="IStringLocalizerFactory"/>.
/// </summary>
internal sealed class ValidationLocalizationSetup(
    IOptions<ValidationLocalizationOptions> localizationOptions,
    IStringLocalizerFactory stringLocalizerFactory,
    IValidationAttributeFormatterProvider attributeFormatterProvider)
    : IConfigureOptions<ValidationOptions>
{
    public void Configure(ValidationOptions options)
    {
        var locOptions = localizationOptions.Value;
        var localizerProvider = locOptions.LocalizerProvider;
        var keySelector = locOptions.ErrorMessageKeySelector;

        options.DisplayNameProvider ??= GetDisplayName;
        options.ErrorMessageProvider ??= GetErrorMessage;

        string? GetDisplayName(in DisplayNameProviderContext context)
        {
            var declaringType = context.DeclaringType ?? typeof(object);
            var localizer = localizerProvider is not null
                ? localizerProvider(declaringType, stringLocalizerFactory)
                : stringLocalizerFactory.Create(declaringType);

            var localized = localizer[context.Name];
            return localized.ResourceNotFound ? null : localized.Value;
        }

        string? GetErrorMessage(in ErrorMessageProviderContext context)
        {
            // Create localizer: per-type or shared, depending on config.
            // IStringLocalizerFactory is responsible for caching IStringLocalizer instances if needed.
            var declaringType = context.DeclaringType ?? typeof(object);
            var localizer = localizerProvider is not null
                ? localizerProvider(declaringType, stringLocalizerFactory)
                : stringLocalizerFactory.Create(declaringType);

            var lookupKey = !string.IsNullOrEmpty(context.Attribute.ErrorMessage)
                ? context.Attribute.ErrorMessage
                : keySelector?.Invoke(context);

            if (lookupKey is null)
            {
                return null;
            }

            var localizedTemplate = localizer[lookupKey];
            if (localizedTemplate.ResourceNotFound)
            {
                return null;
            }

            var displayName = context.DisplayName ?? context.MemberName;

            // Format the localized template with attribute-specific arguments
            var attributeFormatter = attributeFormatterProvider.GetFormatter(context.Attribute);

            return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, displayName)
                ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, displayName);
        }
    }
}
