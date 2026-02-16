// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Configures <see cref="ValidationOptions"/> based on
/// <see cref="ValidationLocalizationOptions"/> and the registered
/// <see cref="IStringLocalizerFactory"/>.
/// </summary>
internal sealed class ValidationLocalizationConfigureOptions(
    IOptions<ValidationLocalizationOptions> localizationOptions,
    IStringLocalizerFactory? stringLocalizerFactory = null,
    IAttributeArgumentProvider? attributeArgumentProvider = null)
    : IConfigureOptions<ValidationOptions>
{
    public void Configure(ValidationOptions options)
    {
        // If no IStringLocalizerFactory is registered, we can't do localization.
        // This shouldn't happen if AddValidationLocalization() was called (which
        // calls AddLocalization()), but we handle it gracefully.
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

            var localized = localizer[context.NameValue];
            return localized.ResourceNotFound ? null : localized.Value;
        }

        string? GetErrorMessage(ErrorMessageContext context)
        {
            // Create localizer: per-type or shared, depending on config
            var declaringType = context.DeclaringType ?? typeof(object);
            var localizer = localizerProvider is not null
                ? localizerProvider(declaringType, stringLocalizerFactory)
                : stringLocalizerFactory.Create(declaringType);

            // Determine the lookup key
            var lookupKey = keySelector is not null
                ? keySelector(context)
                : context.ErrorMessage;

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
            var args = attributeArgumentProvider?.GetFormatArgs(context.Attribute, displayName) ?? [displayName];
            return string.Format(CultureInfo.CurrentCulture, localizedTemplate.Value, args);
        }
    }
}
