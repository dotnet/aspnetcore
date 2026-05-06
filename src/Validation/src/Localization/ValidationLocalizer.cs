// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Resolves localized display names and error messages for the validation pipeline.
/// </summary>
/// <remarks>
/// Configured via <see cref="ValidationOptions"/>. When an <see cref="IStringLocalizerFactory"/> is
/// available, display names and error messages can be resolved against per-declaring-type or shared
/// resource files. <see cref="DisplayAttribute.ResourceType"/>-based display names bypass
/// <see cref="IStringLocalizer"/> and are resolved by <see cref="DisplayAttribute.GetName"/>.
/// </remarks>
public sealed class ValidationLocalizer
{
    private readonly IStringLocalizerFactory? _localizerFactory;
    private readonly Func<Type, IStringLocalizerFactory, IStringLocalizer>? _localizerProvider;
    private readonly Func<ErrorMessageKeyContext, string?>? _keyProvider;
    private readonly ValidationAttributeFormatterRegistry _attributeFormatters;
    private readonly ConcurrentDictionary<Type, IStringLocalizer> _localizerCache = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationLocalizer"/>.
    /// </summary>
    /// <param name="options">The validation options containing the localization configuration.</param>
    /// <param name="factory">The factory used to obtain <see cref="IStringLocalizer"/> instances.
    /// When <see langword="null"/>, only static resource-based display names (via
    /// <see cref="DisplayAttribute.ResourceType"/>) are localized; literal display names and error
    /// messages are returned as-is.</param>
    public ValidationLocalizer(ValidationOptions options, IStringLocalizerFactory? factory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _localizerProvider = options.LocalizerProvider;
        _keyProvider = options.ErrorMessageKeyProvider;
        _attributeFormatters = options.AttributeFormatters;
        _localizerFactory = factory;
    }

    /// <summary>
    /// Resolves the display name for a member, parameter, or type.
    /// </summary>
    /// <remarks>
    /// Resolution order:
    /// <list type="number">
    ///   <item><description>If <paramref name="displayResourceAccessor"/> is non-null, invokes it
    ///   to obtain the resolved name. This handles <see cref="DisplayAttribute.ResourceType"/>-based
    ///   localization (the SG- and runtime-emitted accessors typically delegate to
    ///   <see cref="DisplayAttribute.GetName"/>). <see cref="IStringLocalizer"/> is bypassed in this case.</description></item>
    ///   <item><description>If <paramref name="displayName"/> is non-null and an <see cref="IStringLocalizer"/>
    ///   is configured, looks up the literal value as a key against the per-declaring-type localizer.
    ///   Returns the literal value on miss.</description></item>
    ///   <item><description>If <paramref name="displayName"/> is non-null and no localizer is configured,
    ///   returns the literal value.</description></item>
    ///   <item><description>Otherwise, returns <paramref name="defaultName"/> (typically the member name).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="defaultName">The value to return when neither the resource accessor nor the
    /// literal display name produces a value. Typically the member or parameter name.</param>
    /// <param name="displayName">The literal display name from <see cref="DisplayAttribute.Name"/>
    /// (when no <see cref="DisplayAttribute.ResourceType"/>) or
    /// <see cref="System.ComponentModel.DisplayNameAttribute.DisplayName"/>.</param>
    /// <param name="displayResourceAccessor">An accessor that resolves a localized display name from
    /// a static resource property; <see langword="null"/> when the member is not decorated with a
    /// resource-based <see cref="DisplayAttribute"/>.</param>
    /// <param name="declaringType">The type that declares the member, used as the resource source
    /// for the per-type <see cref="IStringLocalizer"/>; or <see langword="null"/> for parameters.</param>
    /// <returns>The resolved display name. Never <see langword="null"/>.</returns>
    public string ResolveDisplayName(
        string defaultName,
        string? displayName,
        Func<string?>? displayResourceAccessor,
        Type? declaringType)
    {
        // Case 1: resource-based display name (DisplayAttribute.ResourceType path).
        if (displayResourceAccessor is not null)
        {
            return displayResourceAccessor() ?? defaultName;
        }

        // Case 2/3: literal display name from [Display(Name = X)] (no ResourceType) or [DisplayName(X)].
        if (string.IsNullOrEmpty(displayName))
        {
            return defaultName;
        }

        if (_localizerFactory is null)
        {
            return displayName;
        }

        var localizer = GetStringLocalizer(declaringType, _localizerFactory);
        var localizedName = localizer[displayName];

        return localizedName.ResourceNotFound ? displayName : localizedName.Value;
    }

    /// <summary>
    /// Resolves a localized error message for a validation attribute.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when:
    /// <list type="bullet">
    ///   <item><description>no <see cref="IStringLocalizerFactory"/> is configured,</description></item>
    ///   <item><description>the attribute defines its own resource-based error via
    ///   <see cref="ValidationAttribute.ErrorMessageResourceType"/>,</description></item>
    ///   <item><description>no lookup key can be determined (no <see cref="ValidationAttribute.ErrorMessage"/>
    ///   and no <see cref="ValidationOptions.ErrorMessageKeyProvider"/> result), or</description></item>
    ///   <item><description>the localizer reports the resource as not found.</description></item>
    /// </list>
    /// In all of these cases the validation pipeline falls back to the attribute's default
    /// error message.
    /// </remarks>
    /// <param name="attribute">The validation attribute that produced the error.</param>
    /// <param name="displayName">The resolved display name to substitute into the localized template.</param>
    /// <param name="declaringType">The type that declares the member, used as the resource source
    /// for the per-type <see cref="IStringLocalizer"/>; or <see langword="null"/> for parameters.</param>
    /// <returns>The fully formatted localized error message, or <see langword="null"/>.</returns>
    public string? ResolveErrorMessage(ValidationAttribute attribute, string displayName, Type? declaringType)
    {
        if (_localizerFactory is null)
        {
            return null;
        }

        if (attribute.ErrorMessageResourceType is not null)
        {
            return null;
        }

        var lookupKey = !string.IsNullOrEmpty(attribute.ErrorMessage)
            ? attribute.ErrorMessage
            : _keyProvider?.Invoke(new ErrorMessageKeyContext
            {
                Attribute = attribute,
                DisplayName = displayName,
                DeclaringType = declaringType
            });

        if (lookupKey is null)
        {
            return null;
        }

        var localizer = GetStringLocalizer(declaringType, _localizerFactory);
        var localizedTemplate = localizer[lookupKey];

        if (localizedTemplate.ResourceNotFound)
        {
            return null;
        }

        var attributeFormatter = _attributeFormatters.GetFormatter(attribute);

        return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, displayName)
            ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, displayName);
    }

    private IStringLocalizer GetStringLocalizer(Type? type, IStringLocalizerFactory factory)
    {
        var resourceSource = type ?? typeof(object);

        return _localizerCache.GetOrAdd(resourceSource, _localizerProvider is not null
            ? t => _localizerProvider(t, factory)
            : factory.Create);
    }
}
