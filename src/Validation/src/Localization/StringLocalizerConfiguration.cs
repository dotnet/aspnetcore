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
internal sealed class StringLocalizerConfiguration(
    IOptions<ValidationLocalizationOptions> localizationOptions,
    IStringLocalizerFactory? stringLocalizerFactory = null,
    IValidationAttributeFormatterProvider? attributeFormatterProvider = null)
    : IConfigureOptions<ValidationOptions>
{
    public void Configure(ValidationOptions options)
    {
        if (stringLocalizerFactory is null)
        {
            return;
        }

        var locOptions = localizationOptions.Value;
        var localizerProvider = locOptions.LocalizerProvider;
        var keySelector = locOptions.ErrorMessageKeySelector;

        options.DisplayNameProvider ??= GetDisplayName;
        options.ErrorMessageProvider ??= GetErrorMessage;

        string? GetDisplayName(DisplayNameContext context)
        {
            var declaringType = context.DeclaringType ?? typeof(object);
            var localizer = localizerProvider is not null
                ? localizerProvider(declaringType, stringLocalizerFactory)
                : stringLocalizerFactory.Create(declaringType);

            var localized = localizer[context.Name];
            return localized.ResourceNotFound ? null : localized.Value;
        }

        string? GetErrorMessage(ErrorMessageContext context)
        {
            // Create localizer: per-type or shared, depending on config.
            // Caching of IStringLocalizer instances is the responsibility of the IStringLocalizerFactory.
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

            // Look up translation
            var localizedTemplate = localizer[lookupKey];
            if (localizedTemplate.ResourceNotFound)
            {
                return null; // no translation → fall through to default
            }

            var displayName = context.DisplayName ?? context.MemberName;

            // Format the localized template with attribute-specific arguments
            var attributeFormatter = context.Attribute is IValidationAttributeFormatter formatter
                ? formatter
                : attributeFormatterProvider?.GetFormatter(context.Attribute);

            return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, displayName)
                ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, displayName);
        }
    }
}
