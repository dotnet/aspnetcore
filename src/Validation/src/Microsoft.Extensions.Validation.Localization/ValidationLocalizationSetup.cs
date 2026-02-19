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
        var keyProvider = locOptions.ErrorMessageKeyProvider;

        options.DisplayNameProvider ??= GetDisplayName;
        options.ErrorMessageProvider ??= GetErrorMessage;

        string? GetDisplayName(in DisplayNameProviderContext context)
        {
            var localizer = GetLocalizer(context.DeclaringType);
            var localized = localizer[context.Name];
            return localized.ResourceNotFound ? null : localized.Value;
        }

        string? GetErrorMessage(in ErrorMessageProviderContext context)
        {
            var lookupKey = !string.IsNullOrEmpty(context.Attribute.ErrorMessage)
                ? context.Attribute.ErrorMessage
                : keyProvider?.Invoke(context);

            if (lookupKey is null)
            {
                return null;
            }

            // Create localizer: per-type or shared, depending on config.
            // IStringLocalizerFactory is responsible for caching IStringLocalizer instances if needed.
            var localizer = GetLocalizer(context.DeclaringType);
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

        IStringLocalizer GetLocalizer(Type? type)
        {
            var resourceSource = type ?? typeof(object);
            return localizerProvider is not null
                ? localizerProvider(resourceSource, stringLocalizerFactory)
                : stringLocalizerFactory.Create(resourceSource);
        }
    }
}
