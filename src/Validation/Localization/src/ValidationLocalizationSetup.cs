// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Configures <see cref="ValidationOptions"/> based on <see cref="ValidationLocalizationOptions"/>
/// and the registered <see cref="IStringLocalizerFactory"/>.
/// </summary>
internal sealed class ValidationLocalizationSetup(
    IOptions<ValidationLocalizationOptions> localizationOptions,
    IStringLocalizerFactory stringLocalizerFactory,
    IValidationAttributeFormatterProvider attributeFormatterProvider)
    : IConfigureOptions<ValidationOptions>
{
    private readonly ConcurrentDictionary<Type, IStringLocalizer> _localizerCache = new();

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
            var localizedName = localizer[context.Name];
            return localizedName.ResourceNotFound ? null : localizedName.Value;
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

            var localizer = GetLocalizer(context.DeclaringType);
            var localizedTemplate = localizer[lookupKey];

            if (localizedTemplate.ResourceNotFound)
            {
                return null;
            }

            // Format the localized template with attribute-specific arguments
            var attributeFormatter = attributeFormatterProvider.GetFormatter(context.Attribute);

            return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, context.DisplayName)
                ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, context.DisplayName);
        }

        IStringLocalizer GetLocalizer(Type? type)
        {
            var resourceSource = type ?? typeof(object);
            return _localizerCache.GetOrAdd(resourceSource, localizerProvider is not null
                ? t => localizerProvider(t, stringLocalizerFactory)
                : stringLocalizerFactory.Create);
        }
    }
}
