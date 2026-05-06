// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.Localization;

public sealed class ValidationLocalizer
{
    private readonly IStringLocalizerFactory? _localizerFactory;
    private readonly Func<Type, IStringLocalizerFactory, IStringLocalizer>? _localizerProvider;
    private readonly Func<ErrorMessageKeyContext, string?>? _keyProvider;
    private readonly ValidationAttributeFormatterRegistry _attributeFormatters;
    private readonly ConcurrentDictionary<Type, IStringLocalizer> _localizerCache = new();

    public ValidationLocalizer(ValidationOptions options, IStringLocalizerFactory? factory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _localizerProvider = options.LocalizerProvider;
        _keyProvider = options.ErrorMessageKeyProvider;
        _attributeFormatters = options.AttributeFormatters;
        _localizerFactory = factory;
    }

    public string? ResolveDisplayName(string? displayName, Type? displayResource, Type? declaringType)
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            if (displayResource is not null)
            {
                // Read public static property via reflection, throw if not available or not string
            }

            if (_localizerFactory is not null)
            {
                var localizer = GetStringLocalizer(declaringType, _localizerFactory);
                var localizedName = localizer[displayName];

                return localizedName.ResourceNotFound ? displayName : localizedName.Value;
            }
        }

        return displayName;
    }

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
