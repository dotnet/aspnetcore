// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization;

public sealed class ValidationLocalizer
{
    private readonly IStringLocalizerFactory _localizerFactory;
    private readonly ValidationLocalizationOptions _options;

    public ValidationLocalizer(
        IStringLocalizerFactory factory,
        IOptions<ValidationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        _localizerFactory = factory;
        _options = options.Value.Localization;
    }

    /// <summary>
    /// Resolves a localized display name for the member described by <paramref name="context"/>.
    /// </summary>
    /// <param name="context">Information about the member to resolve a display name for.</param>
    /// <returns>The localized display name, or <see langword="null"/> if not available.</returns>
    public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
    {
        if (context.DisplayName is null)
        {
            return null;
        }

        var localizer = GetStringLocalizer(context.Type);
        var localizedName = localizer[context.DisplayName];

        return localizedName.ResourceNotFound ? context.DisplayName : localizedName.Value;
    }

    /// <summary>
    /// Resolves a fully-formatted localized error message for the validation attribute described
    /// by <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations should return <see langword="null"/> when no localized message is available;
    /// the validation pipeline falls back to the attribute's default error message.
    /// </para>
    /// </remarks>
    /// <param name="context">Information about the validation attribute and the member it applied to.</param>
    /// <returns>The fully-formatted localized error message, or <see langword="null"/> to use the
    /// attribute's default message.</returns>
    public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
    {
        // ErrorMessageKeyProvider, when configured, has precedence over Attribute.ErrorMessage.
        // The provider receives the full context (including Attribute.ErrorMessage) and may
        // return a derived key, or return null/empty to defer to using Attribute.ErrorMessage
        // as the key.
        var lookupKey = _options.ErrorMessageKeyProvider?.Invoke(context);
        if (string.IsNullOrEmpty(lookupKey))
        {
            lookupKey = context.Attribute.ErrorMessage;
        }

        if (string.IsNullOrEmpty(lookupKey))
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
        if (_options.LocalizerProvider is { } provider)
        {
            return provider(type, _localizerFactory)
                ?? throw new InvalidOperationException(
                    $"The {nameof(ValidationLocalizationOptions)}.{nameof(ValidationLocalizationOptions.LocalizerProvider)} " +
                    $"delegate returned null for type '{type?.FullName ?? "<null>"}'. " +
                    $"The delegate must return a non-null {nameof(IStringLocalizer)} instance.");
        }

        // No provider configured: fall back to per-type lookup. typeof(object) is the only sensible
        // default at the IStringLocalizerFactory.Create boundary when the pipeline has no declaring
        // type (e.g., top-level Minimal API parameters); applications that need a useful localizer
        // for those scenarios should configure LocalizerProvider explicitly or use
        // AddValidationLocalization<TResource>().
        return _localizerFactory.Create(type ?? typeof(object));
    }
}
