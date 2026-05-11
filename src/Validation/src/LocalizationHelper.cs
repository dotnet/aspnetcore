// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

internal static class LocalizationHelper
{
    internal static string ResolveDisplayName(
        string memberName,
        string? displayName,
        Func<string?>? displayResourceAccessor,
        Type? declaringType,
        IValidationLocalizer? localizer)
    {
        if (displayResourceAccessor is not null)
        {
            // Resource-based display name from [Display(ResourceType = ..., Name = ...)] always
            // wins; the IValidationLocalizer is intentionally bypassed because the resource lookup
            // is the canonical source for the localized name.
            return displayResourceAccessor() ?? memberName;
        }

        if (localizer is null)
        {
            // No localizer configured: use the literal display name, or fall back to the member name.
            return displayName ?? memberName;
        }

        // Delegate to the configured IValidationLocalizer.
        var displayNameContext = new DisplayNameLocalizationContext
        {
            DeclaringType = declaringType,
            DisplayName = displayName,
            MemberName = memberName,
        };

        return localizer.ResolveDisplayName(displayNameContext) ?? memberName;
    }

    internal static string? ResolveAttributeErrorMessage(
        string memberName,
        string displayName,
        Type? declaringType,
        ValidationAttribute attribute,
        ValidationResult result,
        IValidationLocalizer? localizer)
    {
        // Skip the IValidationLocalizer when:
        //   * no localizer is configured, or
        //   * the attribute already resolves its message via ErrorMessageResourceType, in which
        //     case DataAnnotations has already produced the localized message in result.ErrorMessage.
        if (localizer is null || attribute.ErrorMessageResourceType is not null)
        {
            return result.ErrorMessage;
        }

        return localizer.ResolveErrorMessage(new()
        {
            MemberName = memberName,
            DisplayName = displayName,
            DeclaringType = declaringType,
            Attribute = attribute,
        }) ?? result.ErrorMessage;
    }
}
