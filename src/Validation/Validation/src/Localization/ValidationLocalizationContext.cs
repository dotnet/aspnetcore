// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Encapsulates IStringLocalizer-based localization state for the validation pipeline.
/// Created at configuration time by <see cref="ValidationLocalizationAutoSetup"/> when
/// <see cref="IStringLocalizerFactory"/> is available in DI.
/// </summary>
internal sealed class ValidationLocalizationContext
{
    private readonly IStringLocalizerFactory _factory;
    private readonly Func<Type, IStringLocalizerFactory, IStringLocalizer>? _localizerProvider;
    private readonly Func<ErrorMessageKeyContext, string?>? _keyProvider;
    private readonly ValidationAttributeFormatterRegistry _formatters;
    private readonly ConcurrentDictionary<Type, IStringLocalizer> _localizerCache = new();

    internal ValidationLocalizationContext(
        IStringLocalizerFactory factory,
        Func<Type, IStringLocalizerFactory, IStringLocalizer>? localizerProvider,
        Func<ErrorMessageKeyContext, string?>? keyProvider,
        ValidationAttributeFormatterRegistry formatters)
    {
        _factory = factory;
        _localizerProvider = localizerProvider;
        _keyProvider = keyProvider;
        _formatters = formatters;
    }

    internal string? ResolveDisplayName(string name, Type? declaringType)
    {
        var localizer = GetLocalizer(declaringType);
        var localizedName = localizer[name];

        return localizedName.ResourceNotFound ? null : localizedName.Value;
    }

    internal string? ResolveErrorMessage(ValidationAttribute attribute, string memberName, string displayName, Type? declaringType)
    {
        var lookupKey = !string.IsNullOrEmpty(attribute.ErrorMessage)
            ? attribute.ErrorMessage
            : _keyProvider?.Invoke(new ErrorMessageKeyContext
            {
                Attribute = attribute,
                MemberName = memberName,
                DisplayName = displayName,
                DeclaringType = declaringType,
            });

        if (lookupKey is null)
        {
            return null;
        }

        var localizer = GetLocalizer(declaringType);
        var localizedTemplate = localizer[lookupKey];

        if (localizedTemplate.ResourceNotFound)
        {
            return null;
        }

        var attributeFormatter = _formatters.GetFormatter(attribute);

        return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, displayName)
            ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, displayName);
    }

    private IStringLocalizer GetLocalizer(Type? type)
    {
        var resourceSource = type ?? typeof(object);

        return _localizerCache.GetOrAdd(resourceSource, _localizerProvider is not null
            ? t => _localizerProvider(t, _factory)
            : _factory.Create);
    }
}
