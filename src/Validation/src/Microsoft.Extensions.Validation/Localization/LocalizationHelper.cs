// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

internal static class LocalizationHelper
{
    internal static string ResolveDisplayName(
        DisplayAttribute? displayAttribute,
        Type? declaringType,
        string memberName,
        DisplayNameProvider? provider,
        IServiceProvider services)
    {
        if (displayAttribute is DisplayAttribute display && display.GetName() is string displayName)
        {
            if (display.ResourceType is not null)
            {
                // Display name is localized via a static property (typically generated from a resource file).
                return displayName;
            }

            if (provider is null)
            {
                // Run-time localization is not set up. The Name value is used directly. 
                return displayName;
            }

            // Display name is localized using run-time localization.
            var displayNameContext = new DisplayNameProviderContext
            {
                DeclaringType = declaringType,
                Name = displayName,
                Services = services
            };

            return provider(displayNameContext) ?? displayName;
        }

        return memberName;
    }

    /// <summary>
    /// Attempts to resolve a localized/customized error message for a validation attribute.
    /// Returns null if no provider is configured, the attribute uses its own resource-based
    /// localization, or the provider returns null (indicating fallback to default behavior).
    /// </summary>
    /// <param name="attribute">The validation attribute that produced the error.</param>
    /// <param name="declaringType">The declaring type, or null for parameters.</param>
    /// <param name="displayName">The (possibly localized) display name of the member.</param>
    /// <param name="memberName">The CLR member name.</param>
    /// <param name="provider">The delegate that resolves error messages for validation attributes.</param>
    /// <param name="services">The service provider for resolving localization services.</param>
    /// <returns>The resolved error message, or null to fall through to default behavior.</returns>
    internal static string? TryResolveErrorMessage(
        ValidationAttribute attribute,
        Type? declaringType,
        string? displayName,
        string memberName,
        ErrorMessageProvider? provider,
        IServiceProvider services)
    {
        if (attribute.ErrorMessageResourceType is not null)
        {
            // Error message is localized via a static property (typically generated from a resource file).
            // Error message is retrieved from the ValidationResult directly.
            return null;
        }

        if (provider is null)
        {
            // Run-time localization is not set up.
            // Error message is retrieved from the ValidationResult directly.
            return null;
        }

        // Error message is localized using run-time localization.
        var errorMessageContext = new ErrorMessageProviderContext
        {
            Attribute = attribute,
            DisplayName = displayName,
            MemberName = memberName,
            DeclaringType = declaringType,
            Services = services
        };

        return provider(errorMessageContext);
    }
}
