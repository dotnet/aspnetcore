// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Localization;

internal static class LocalizationHelper
{
    // ErrorMessageString is an internal property on ValidationAttribute that returns
    // the effective error message template (from ErrorMessage, resource, or built-in default).
    private static readonly PropertyInfo? _errorMessageStringProperty =
        typeof(ValidationAttribute).GetProperty(
            "ErrorMessageString",
            BindingFlags.Instance | BindingFlags.NonPublic);

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    internal static string ResolveDisplayName(DisplayAttribute? displayAttribute, Type? declaringType, string memberName, ValidateContext context)
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        if (displayAttribute is DisplayAttribute display && display.GetName() is string displayName)
        {
            if (display.ResourceType is not null)
            {
                // Name is loaded from a resource file.
                return displayName;
            }
            else
            {
                // Name is localized via key.
                var displayNameProvider = context.DisplayNameProvider ?? context.ValidationOptions.DisplayNameProvider;
                var localizedDisplayName = LocalizeDisplayName(declaringType, displayName, displayNameProvider, context.ValidationContext);
                return localizedDisplayName;
            }
        }

        return memberName;
    }

    /// <summary>
    /// Resolves the display name for a member, using the DisplayNameResolver if configured.
    /// </summary>
    /// <param name="declaringType">The type that declares the member, or null for parameters.</param>
    /// <param name="displayName">The value specified in the Name property of the DisplayAttribute, or the CLR name if no DisplayAttribute.</param>
    /// <param name="provider">The delegate that resolves display names for properties and parameters.</param>
    /// <param name="services">The service provider for resolving localization services.</param>
    /// <returns>The resolved display name.</returns>
    private static string LocalizeDisplayName(
        Type? declaringType,
        string displayName,
        Func<DisplayNameContext, string?>? provider,
        IServiceProvider services)
    {
        if (provider is null)
        {
            return displayName;
        }

        var displayNameContext = new DisplayNameContext
        {
            DeclaringType = declaringType,
            Name = displayName,
            Services = services
        };

        return provider(displayNameContext) ?? displayName;
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
        Func<ErrorMessageContext, string?>? provider,
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

        var (template, isCustom) = GetErrorMessage(attribute);
        if (template is null)
        {
            return null;
        }

        var errorMessageContext = new ErrorMessageContext
        {
            Attribute = attribute,
            ErrorMessage = template,
            IsCustomErrorMessage = isCustom,
            DisplayName = displayName,
            MemberName = memberName,
            DeclaringType = declaringType,
            Services = services
        };

        return provider(errorMessageContext);
    }

    /// <summary>
    /// Gets the error message template for a ValidationAttribute without mutating it.
    /// Returns the explicit ErrorMessage if set, otherwise the attribute's built-in default.
    /// </summary>
    private static (string? template, bool isCustom) GetErrorMessage(
        ValidationAttribute attribute)
    {
        // If the user explicitly set ErrorMessage, use it as the lookup key.
        if (attribute.ErrorMessage is not null)
        {
            return (attribute.ErrorMessage, isCustom: true);
        }

        // Fast path: well-known BCL attributes
        if (TryGetWellKnownAttributeMessage(attribute) is string knownMessage)
        {
            return (knownMessage, isCustom: false);
        }

        // Slow path: use reflection to read ErrorMessageString
        // This gives us the default template for custom/unknown attributes.
        // Note that we cannot cache this because attributes can have dynamic messages based on run-time values.
        try
        {
            var message = _errorMessageStringProperty?.GetValue(attribute) as string;
            return (message, isCustom: false);
        }
        catch
        {
            return (null, false);
        }
    }

    /// <summary>
    /// Well-known default error message templates for BCL validation attributes.
    /// These match the strings in System.ComponentModel.Annotations SR.resources.
    /// Used as a fast-path to avoid reflection for common attributes.
    /// </summary>
    private static string? TryGetWellKnownAttributeMessage(ValidationAttribute attribute) => attribute switch
    {
        RequiredAttribute =>
            "The {0} field is required.",
        RangeAttribute =>
            "The field {0} must be between {1} and {2}.",
        StringLengthAttribute { MinimumLength: > 0 } =>
            "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.",
        StringLengthAttribute =>
            "The field {0} must be a string with a maximum length of {1}.",
        MinLengthAttribute =>
            "The field {0} must be a string or array type with a minimum length of '{1}'.",
        MaxLengthAttribute =>
            "The field {0} must be a string or array type with a maximum length of '{1}'.",
        RegularExpressionAttribute =>
            "The field {0} must match the regular expression '{1}'.",
        CompareAttribute =>
            "'{0}' and '{1}' do not match.",
        EmailAddressAttribute =>
            "The {0} field is not a valid e-mail address.",
        PhoneAttribute =>
            "The {0} field is not a valid phone number.",
        CreditCardAttribute =>
            "The {0} field is not a valid credit card number.",
        UrlAttribute =>
            "The {0} field is not a valid fully-qualified http, https, or ftp URL.",
        FileExtensionsAttribute =>
            "The {0} field only accepts files with the following extensions: {1}",
        LengthAttribute =>
            "The field {0} must have a length between '{1}' and '{2}'.",
        Base64StringAttribute =>
            "The {0} field is not a valid Base64 encoding.",
        AllowedValuesAttribute =>
            "The {0} field does not equal any of the values specified in AllowedValuesAttribute.",
        DeniedValuesAttribute =>
            "The {0} field equals one of the values specified in DeniedValuesAttribute.",
        _ => null,
    };
}
