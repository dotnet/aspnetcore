// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

internal static class LocalizationHelper
{
    internal static string ResolveDisplayName(
        string memberName,
        string? displayName,
        Func<string>? displayNameAccessor,
        Type? declaringType,
        ValidationLocalizationContext? localization)
    {
        if (displayNameAccessor?.Invoke() is string resourceDisplayName)
        {
            // Display name is localized via a static property (typically generated from a resource file).
            return resourceDisplayName;
        }

        if (displayName is not null && localization is not null)
        {
            // Display name is localized using IStringLocalizer.
            return localization.ResolveDisplayName(displayName, declaringType) ?? displayName;
        }

        // No localization configured or no display name set.
        return displayName ?? memberName;
    }

    /// <summary>
    /// Attempts to resolve a localized/customized error message for a validation attribute.
    /// Returns null if localization is not configured, the attribute uses its own resource-based
    /// localization, or the localization lookup returns null (indicating fallback to default behavior).
    /// </summary>
    internal static string? TryResolveErrorMessage(
        ValidationAttribute attribute,
        Type? declaringType,
        string displayName,
        ValidationLocalizationContext? localization)
    {
        if (attribute.ErrorMessageResourceType is not null)
        {
            // Error message is localized via a static property (typically generated from a resource file).
            return null;
        }

        // Error message is localized using IStringLocalizer.
        return localization?.ResolveErrorMessage(attribute, displayName, declaringType);
    }
}
