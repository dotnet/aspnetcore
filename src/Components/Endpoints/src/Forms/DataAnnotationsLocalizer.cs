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
       string? memberName,
       string displayName,
       Type type,
       ValidationAttribute attribute,
       bool useStringLocalizer)
    {
        if (!useStringLocalizer || localizerFactory is null || attribute.ErrorMessageResourceType is not null)
        {
            return attribute.FormatErrorMessage(displayName);
        }

        var localizer = GetStringLocalizer(type, localizerFactory);
        var localizedTemplate = FindLocalizedTemplate(localizer, attribute, memberName, type);

        if (localizedTemplate is null)
        {
            return attribute.FormatErrorMessage(displayName);
        }

        // Format the localized template with attribute-specific arguments
        return FormatMessage(attribute, CultureInfo.CurrentCulture, localizedTemplate, displayName);
    }

    // Resolves the localized message template for a validation attribute.
    //
    // An explicit ErrorMessage is used verbatim as the lookup key. Otherwise the built-in key
    // convention is applied, walking from the most specific key to the least specific one:
    //
    //   {DeclaringType}_{MemberName}_{AttributeType}_Error
    //   {DeclaringType}_{AttributeType}_Error
    //   {AttributeType}_Error
    //
    // Returns null when no key resolves, in which case the caller falls back to the
    // non-localized message produced by the attribute itself.
    //
    // Keep in sync with the generated LocalizationHelpers.FindLocalizedTemplate in
    // src/Validation/gen/Templates/LocalizationHelpers.cs.
    private static string? FindLocalizedTemplate(
        IStringLocalizer localizer,
        ValidationAttribute attribute,
        string? memberName,
        Type declaringType)
    {
        if (!string.IsNullOrEmpty(attribute.ErrorMessage))
        {
            var explicitMatch = localizer[attribute.ErrorMessage];
            if (!explicitMatch.ResourceNotFound)
            {
                return explicitMatch.Value;
            }
        }

        var attributeName = attribute.GetType().Name;
        var typeName = GetKeySegment(declaringType);

        // The member-specific tier is skipped when there is no member to key on, which is the case
        // for a type-level attribute that reports no member names.
        if (memberName is not null)
        {
            var memberMatch = localizer[$"{typeName}_{memberName}_{attributeName}_Error"];
            if (!memberMatch.ResourceNotFound)
            {
                return memberMatch.Value;
            }
        }

        var typeMatch = localizer[$"{typeName}_{attributeName}_Error"];
        if (!typeMatch.ResourceNotFound)
        {
            return typeMatch.Value;
        }

        var globalMatch = localizer[$"{attributeName}_Error"];

        return globalMatch.ResourceNotFound ? null : globalMatch.Value;
    }

    private static string GetKeySegment(Type type)
    {
        var name = (Nullable.GetUnderlyingType(type) ?? type).Name;
        var arityIndex = name.IndexOf('`');

        return arityIndex < 0 ? name : name[..arityIndex];
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
