// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal class DataAnnotationsLocalizer(ValidationOptions options, IStringLocalizerFactory? localizerFactory)
{
    // Mirrors the decision tree used by the server-side validation.
    // Resource-attribute display names bypass the localizer (resource lookup is the canonical
    // localized source). Literal display names act as both lookup key and fallback for the localizer.
    public string ResolveDisplayName(in ClientValidationFieldMetadata metadata, bool useStringLocalizer)
    {
        if (metadata.ResourceDisplayAttribute is { } resourceAttribute)
        {
            return resourceAttribute.GetName() ?? metadata.PropertyName;
        }

        if (metadata.LiteralDisplayName is not { } literal)
        {
            return metadata.PropertyName;
        }

        if (!useStringLocalizer || localizerFactory is null)
        {
            return literal;
        }

        var localizer = GetStringLocalizer(metadata.DeclaringType, localizerFactory);
        var localizedTemplate = localizer[literal];

        return localizedTemplate.ResourceNotFound ? literal : localizedTemplate.Value;
    }

    // Mirrors the decision tree used by the server-side validation. Falls back to
    // FormatErrorMessage when no localizer is configured or the attribute already supplies
    // resource-based localization.
    //
    // Keep in sync with the generated ResolveAttributeErrorMessage/FormatErrorMessage in
    // src/Validation/gen/Templates/ValidatableInfo.cs, which this mirrors for the SSR client payload.
    public string? ResolveAttributeErrorMessage(
       string memberName,
       string displayName,
       Type type,
       ValidationAttribute attribute,
       bool useStringLocalizer)
    {
        if (!useStringLocalizer || localizerFactory is null || attribute.ErrorMessageResourceType is not null)
        {
            return attribute.FormatErrorMessage(displayName);
        }

        var lookupKey = GetErrorMessageKey(attribute, memberName, type);

        if (string.IsNullOrEmpty(lookupKey))
        {
            return attribute.FormatErrorMessage(displayName);
        }

        var localizer = GetStringLocalizer(type, localizerFactory);
        var localizedTemplate = localizer[lookupKey];

        if (localizedTemplate.ResourceNotFound)
        {
            return attribute.FormatErrorMessage(displayName);
        }

        // Format the localized template with attribute-specific arguments
        return FormatMessage(attribute, CultureInfo.CurrentCulture, localizedTemplate.Value, displayName);
    }

    private string? GetErrorMessageKey(ValidationAttribute attribute, string memberName, Type type)
    {
        if (!string.IsNullOrEmpty(attribute.ErrorMessage))
        {
            return attribute.ErrorMessage;
        }

        return options.MessageKeyProvider?.Invoke(new ValidationMessageKeyContext
        {
            ValidatorType = attribute.GetType(),
            MemberName = memberName,
            DeclaringType = type,
        });
    }

    private IStringLocalizer GetStringLocalizer(Type type, IStringLocalizerFactory localizerFactory)
        => options.LocalizerProvider(type, localizerFactory)
            ?? throw new InvalidOperationException(
                $"The {nameof(ValidationOptions)}.{nameof(ValidationOptions.LocalizerProvider)} " +
                $"delegate returned null for type '{type.FullName}'. " +
                $"The delegate must return a non-null {nameof(IStringLocalizer)} instance.");

    private static string FormatMessage(ValidationAttribute attribute, CultureInfo culture, string messageTemplate, string displayName)
        => attribute switch
        {
            IValidationMessageFormatter selfFormatter => selfFormatter.FormatMessage(culture, messageTemplate, displayName),
            CompareAttribute a => string.Format(culture, messageTemplate, displayName, a.OtherPropertyDisplayName ?? a.OtherProperty),
            FileExtensionsAttribute a => string.Format(culture, messageTemplate, displayName, a.Extensions),
            LengthAttribute a => string.Format(culture, messageTemplate, displayName, a.MinimumLength, a.MaximumLength),
            MaxLengthAttribute a => string.Format(culture, messageTemplate, displayName, a.Length),
            MinLengthAttribute a => string.Format(culture, messageTemplate, displayName, a.Length),
            RangeAttribute a => string.Format(culture, messageTemplate, displayName, a.Minimum, a.Maximum),
            RegularExpressionAttribute a => string.Format(culture, messageTemplate, displayName, a.Pattern),
            StringLengthAttribute a => string.Format(culture, messageTemplate, displayName, a.MaximumLength, a.MinimumLength),
            _ => string.Format(culture, messageTemplate, displayName),
        };
}
