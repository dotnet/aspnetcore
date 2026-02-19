// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

internal static class LocalizationHelper
{
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    internal static string ResolveDisplayName(DisplayAttribute? displayAttribute, Type? declaringType, string memberName, ValidateContext context)
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        if (displayAttribute is DisplayAttribute display && display.GetName() is string displayName)
        {
            if (display.ResourceType is not null)
            {
                // Name is localized via static property (typically generated from a resource file).
                return displayName;
            }
            else
            {
                // Name is (optionally) localized using the Name value as a key.
                var displayNameProvider = context.DisplayNameProvider ?? context.ValidationOptions.DisplayNameProvider;

                if (displayNameProvider is null)
                {
                    return displayName;
                }

                var displayNameContext = new DisplayNameProviderContext
                {
                    DeclaringType = declaringType,
                    Name = displayName,
                    Services = context.ValidationContext
                };

                return displayNameProvider(displayNameContext) ?? displayName;
            }
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
        // If the attribute uses its own resource-based localization
        // (ErrorMessageResourceType is set), skip external localization
        // to avoid double-localization.
        if (attribute.ErrorMessageResourceType is not null)
        {
            return null;
        }

        if (provider is null)
        {
            return null;
        }

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
