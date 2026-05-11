// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization;

internal sealed class DefaultValidationLocalizer : IValidationLocalizer
{
    private readonly IStringLocalizerFactory _localizerFactory;
    private readonly ValidationLocalizationOptions _options;

    public DefaultValidationLocalizer(IStringLocalizerFactory factory, IOptions<ValidationLocalizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        _localizerFactory = factory;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
    {
        if (context.DisplayName is null)
        {
            return null;
        }

        var localizer = GetStringLocalizer(context.DeclaringType);
        var localizedName = localizer[context.DisplayName];

        return localizedName.ResourceNotFound ? context.DisplayName : localizedName.Value;
    }

    /// <inheritdoc/>
    public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
    {
        var lookupKey = !string.IsNullOrEmpty(context.Attribute.ErrorMessage)
            ? context.Attribute.ErrorMessage
            : _options.ErrorMessageKeyProvider?.Invoke(context);

        if (lookupKey is null)
        {
            return null;
        }

        var localizer = GetStringLocalizer(context.DeclaringType);
        var localizedTemplate = localizer[lookupKey];

        if (localizedTemplate.ResourceNotFound)
        {
            return null;
        }

        // Format the localized template with attribute-specific arguments
        var attributeFormatter = _options.AttributeFormatters.GetFormatter(context.Attribute);

        return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, context.DisplayName)
            ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, context.DisplayName);
    }

    private IStringLocalizer GetStringLocalizer(Type? type)
    {
        var resourceSource = type ?? typeof(object);

        if (_options.LocalizerProvider is null)
        {
            return _localizerFactory.Create(resourceSource);
        }

        return _options.LocalizerProvider(resourceSource, _localizerFactory)
            ?? throw new InvalidOperationException(
                $"The {nameof(ValidationLocalizationOptions)}.{nameof(ValidationLocalizationOptions.LocalizerProvider)} delegate returned null for type '{resourceSource.FullName}'. " +
                $"The delegate must return a non-null {nameof(IStringLocalizer)} instance.");
    }
}
