// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.Extensions.Validation.Localization;

internal static class LocalizationHelper
{
    // ── Default error message template cache ──
    // Maps attribute type → its built-in default error message template string.
    // Lazily populated on first access per attribute type.
    private static readonly ConcurrentDictionary<Type, string?> _defaultTemplateCache = new();

    // ErrorMessageString is an internal property on ValidationAttribute that returns
    // the effective error message template (from ErrorMessage, resource, or built-in default).
    private static readonly PropertyInfo? _errorMessageStringProperty =
        typeof(ValidationAttribute).GetProperty(
            "ErrorMessageString",
            BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// Well-known default error message templates for BCL validation attributes.
    /// These match the strings in System.ComponentModel.Annotations SR.resources.
    /// Used as a fast-path to avoid reflection for common attributes.
    /// </summary>
    private static readonly Dictionary<Type, string> _wellKnownDefaults = new()
    {
        [typeof(RequiredAttribute)] =
            "The {0} field is required.",
        [typeof(RangeAttribute)] =
            "The field {0} must be between {1} and {2}.",
        [typeof(StringLengthAttribute)] =
            "The field {0} must be a string with a maximum length of {1}.",
        [typeof(MinLengthAttribute)] =
            "The field {0} must be a string or array type with a minimum length of '{1}'.",
        [typeof(MaxLengthAttribute)] =
            "The field {0} must be a string or array type with a maximum length of '{1}'.",
        [typeof(RegularExpressionAttribute)] =
            "The field {0} must match the regular expression '{1}'.",
        [typeof(CompareAttribute)] =
            "'{0}' and '{1}' do not match.",
        [typeof(EmailAddressAttribute)] =
            "The {0} field is not a valid e-mail address.",
        [typeof(PhoneAttribute)] =
            "The {0} field is not a valid phone number.",
        [typeof(CreditCardAttribute)] =
            "The {0} field is not a valid credit card number.",
        [typeof(UrlAttribute)] =
            "The {0} field is not a valid fully-qualified http, https, or ftp URL.",
        [typeof(FileExtensionsAttribute)] =
            "The {0} field only accepts files with the following extensions: {1}",
        [typeof(LengthAttribute)] =
            "The field {0} must have a length between '{1}' and '{2}'.",
        [typeof(Base64StringAttribute)] =
            "The {0} field is not a valid Base64 encoding.",
        [typeof(AllowedValuesAttribute)] =
            "The {0} field does not equal any of the values specified in AllowedValuesAttribute.",
        [typeof(DeniedValuesAttribute)] =
            "The {0} field equals one of the values specified in DeniedValuesAttribute.",
    };

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    internal static string ResolveDisplayName(DisplayAttribute? displayAttribute, Type? declaringType, string defaultName, ValidateContext context)
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        if (displayAttribute is DisplayAttribute display && display.GetName() is string displayNameValue)
        {
            if (display.ResourceType is not null)
            {
                // Name is loaded from a resource file.
                return displayNameValue;
            }
            else
            {
                // Name is localized via key.
                var displayNameProvider = context.DisplayNameProvider ?? context.ValidationOptions.DisplayNameProvider;
                var localizedDisplayName = GetDisplayName(declaringType, displayNameValue, displayNameProvider);
                return localizedDisplayName;
            }
        }

        return defaultName;
    }

    /// <summary>
    /// Resolves the display name for a member, using the DisplayNameResolver if configured.
    /// </summary>
    /// <param name="declaringType">The type that declares the member, or null for parameters.</param>
    /// <param name="nameValue">The value specified in the Name property of the DisplayAttribute.</param>
    /// <param name="provider">The delegate that resolves display names for properties and parameters.</param>
    /// <returns>The resolved display name.</returns>
    private static string GetDisplayName(
        Type? declaringType,
        string nameValue,
        Func<DisplayNameContext, string?>? provider)
    {
        if (provider is null)
        {
            return nameValue;
        }

        var displayNameContext = new DisplayNameContext
        {
            DeclaringType = declaringType,
            NameValue = nameValue,
        };

        return provider(displayNameContext) ?? nameValue;
    }

    /// <summary>
    /// Attempts to resolve a localized/customized error message for a validation attribute.
    /// Returns null if no provider is configured, the attribute uses its own resource-based
    /// localization, or the provider returns null (indicating fallback to default behavior).
    /// </summary>
    /// <param name="attribute">The validation attribute that produced the error.</param>
    /// <param name="declaringType">The declaring type, or null for parameters.</param>
    /// <param name="displayName">The (possibly localized) display name of the member.</param>
    /// <param name="provider">The delegate that resolves error messages for validation attributes.</param>
    /// <returns>The resolved error message, or null to fall through to default behavior.</returns>
    internal static string? TryResolveErrorMessage(
        ValidationAttribute attribute,
        Type? declaringType,
        string displayName,
        Func<ErrorMessageContext, string?>? provider)
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

        var (template, isCustom) = GetErrorMessageTemplate(attribute);
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
            DeclaringType = declaringType,
        };

        return provider(errorMessageContext);
    }

    /// <summary>
    /// Gets the error message template for a ValidationAttribute without mutating it.
    /// Returns the explicit ErrorMessage if set, otherwise the attribute's built-in default.
    /// </summary>
    private static (string? template, bool isCustom) GetErrorMessageTemplate(
        ValidationAttribute attribute)
    {
        // If the user explicitly set ErrorMessage, use it as the lookup key.
        if (attribute.ErrorMessage is not null)
        {
            return (attribute.ErrorMessage, isCustom: true);
        }

        // Otherwise, try to get the built-in default template.
        var attributeType = attribute.GetType();

        var defaultTemplate = _defaultTemplateCache.GetOrAdd(attributeType, static (type, attr) =>
        {
            // Fast path: well-known BCL attributes
            if (_wellKnownDefaults.TryGetValue(type, out var known))
            {
                return known;
            }

            // Slow path: use reflection to read ErrorMessageString
            // This gives us the default template for custom/unknown attributes.
            try
            {
                return _errorMessageStringProperty?.GetValue(attr) as string;
            }
            catch
            {
                return null;
            }
        }, attribute);

        return (defaultTemplate, isCustom: false);
    }
}
