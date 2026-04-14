// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

using CacheKey = (Type ModelType, string FieldName, string Culture);

internal sealed class DefaultClientValidationService(
    IOptions<ValidationOptions> validationOptions) : IClientValidationService
{
    private readonly ConcurrentDictionary<CacheKey, IReadOnlyDictionary<string, object>?> _cache = new();

    public IReadOnlyDictionary<string, object>? GetHtmlAttributes(FieldIdentifier fieldIdentifier)
    {
        var options = validationOptions.Value;
        var culture = CultureInfo.CurrentUICulture.Name;
        var cacheKey = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName, culture);
        return _cache.GetOrAdd(cacheKey, key => ComputeAttributes(key.ModelType, key.FieldName, options));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Model types are application code and are preserved by default.")]
    private static Dictionary<string, object>? ComputeAttributes(Type modelType, string fieldName, ValidationOptions options)
    {
        var property = modelType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true);
        var htmlAttributes = new Dictionary<string, object>();
        var displayName = GetDisplayName(property, modelType, options);

        foreach (var validationAttribute in validationAttributes)
        {
            var errorMessage = GetErrorMessage(validationAttribute, fieldName, displayName, modelType, options);
            AddAttributes(htmlAttributes, validationAttribute, errorMessage);
        }

        if (htmlAttributes.Count == 0)
        {
            return null;
        }

        htmlAttributes.TryAdd("data-val", "true");
        return htmlAttributes;
    }

    private static void AddAttributes(
        Dictionary<string, object> htmlAttributes,
        ValidationAttribute validationAttribute,
        string errorMessage)
    {
        switch (validationAttribute)
        {
            case RequiredAttribute:
                htmlAttributes.TryAdd("data-val-required", errorMessage);
                break;

            case StringLengthAttribute sla:
                htmlAttributes.TryAdd("data-val-length", errorMessage);
                if (sla.MaximumLength != int.MaxValue)
                {
                    htmlAttributes.TryAdd("data-val-length-max", sla.MaximumLength.ToString(CultureInfo.InvariantCulture));
                }
                if (sla.MinimumLength != 0)
                {
                    htmlAttributes.TryAdd("data-val-length-min", sla.MinimumLength.ToString(CultureInfo.InvariantCulture));
                }
                break;

            case MaxLengthAttribute mla:
                htmlAttributes.TryAdd("data-val-maxlength", errorMessage);
                htmlAttributes.TryAdd("data-val-maxlength-max", mla.Length.ToString(CultureInfo.InvariantCulture));
                break;

            case MinLengthAttribute mla:
                htmlAttributes.TryAdd("data-val-minlength", errorMessage);
                htmlAttributes.TryAdd("data-val-minlength-min", mla.Length.ToString(CultureInfo.InvariantCulture));
                break;

            case RangeAttribute ra:
                ra.IsValid(3); // Trigger internal conversion of Minimum/Maximum
                htmlAttributes.TryAdd("data-val-range", errorMessage);
                htmlAttributes.TryAdd("data-val-range-min", Convert.ToString(ra.Minimum, CultureInfo.InvariantCulture)!);
                htmlAttributes.TryAdd("data-val-range-max", Convert.ToString(ra.Maximum, CultureInfo.InvariantCulture)!);
                break;

            case RegularExpressionAttribute rea:
                htmlAttributes.TryAdd("data-val-regex", errorMessage);
                htmlAttributes.TryAdd("data-val-regex-pattern", rea.Pattern);
                break;

            case CompareAttribute ca:
                htmlAttributes.TryAdd("data-val-equalto", errorMessage);
                htmlAttributes.TryAdd("data-val-equalto-other", "*." + ca.OtherProperty);
                break;

            case EmailAddressAttribute:
                htmlAttributes.TryAdd("data-val-email", errorMessage);
                break;

            case UrlAttribute:
                htmlAttributes.TryAdd("data-val-url", errorMessage);
                break;

            case PhoneAttribute:
                htmlAttributes.TryAdd("data-val-phone", errorMessage);
                break;

            case CreditCardAttribute:
                htmlAttributes.TryAdd("data-val-creditcard", errorMessage);
                break;

            case FileExtensionsAttribute fea:
                htmlAttributes.TryAdd("data-val-fileextensions", errorMessage);
                var normalizedExtensions = fea.Extensions
                    .Replace(" ", string.Empty)
                    .Replace(".", string.Empty)
                    .ToLowerInvariant();
                var parsedExtensions = normalizedExtensions
                    .Split(',')
                    .Select(e => "." + e);
                htmlAttributes.TryAdd("data-val-fileextensions-extensions", string.Join(",", parsedExtensions));
                break;

            default:
                // Check for custom adapter on the attribute
                if (validationAttribute is IClientValidationAdapter adapter)
                {
                    var context = new ClientValidationContext(htmlAttributes, errorMessage);
                    adapter.AddClientValidation(in context);
                }
                break;
        }
    }

    private static string GetDisplayName(PropertyInfo property, Type declaringType, ValidationOptions options)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();

        if (displayAttribute is not null)
        {
            // If ResourceType is set, use the static resource accessor (already localized)
            if (displayAttribute.ResourceType is not null)
            {
                return displayAttribute.GetName() ?? property.Name;
            }

            // Use the localization pipeline if available
            if (displayAttribute.Name is not null)
            {
                return options.ResolveDisplayName(displayAttribute.Name, declaringType);
            }
        }

        var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();

        if (displayNameAttribute is not null)
        {
            return displayNameAttribute.DisplayName;
        }

        return property.Name;
    }

    private static string GetErrorMessage(
        ValidationAttribute validationAttribute,
        string memberName,
        string displayName,
        Type declaringType,
        ValidationOptions options)
    {
        // Try localization pipeline first
        var localizedMessage = options.FormatErrorMessage(validationAttribute, memberName, displayName, declaringType);
        if (localizedMessage is not null)
        {
            return localizedMessage;
        }

        // Fall back to the attribute's own formatting
        return validationAttribute.FormatErrorMessage(displayName);
    }
}
