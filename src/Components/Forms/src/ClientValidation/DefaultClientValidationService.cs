// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

using CacheKey = (Type ModelType, string FieldName);

/// <summary>
/// Default implementation that generates <c>data-val-*</c> HTML attributes from
/// <see cref="ValidationAttribute"/>s found via reflection on model properties.
/// Registered as a singleton - results are cached per (Type, FieldName) pair.
/// </summary>
internal sealed class DefaultClientValidationService : IClientValidationService
{
    // Cache keyed by model type + field name. Safe for concurrent access since the service
    // is a singleton shared across requests. The computed dictionaries are immutable after creation.
    private readonly ConcurrentDictionary<CacheKey, IReadOnlyDictionary<string, object>?> _cache = new();

    public IReadOnlyDictionary<string, object>? GetClientValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        var cacheKey = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
        return _cache.GetOrAdd(cacheKey, static key => ComputeAttributes(key.ModelType, key.FieldName));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Model types are application code and are preserved by default.")]
    private static Dictionary<string, object>? ComputeAttributes(Type modelType, string fieldName)
    {
        var property = modelType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true);
        var htmlAttributes = new Dictionary<string, object>();
        var displayName = GetDisplayName(property) ?? fieldName;

        foreach (var validationAttribute in validationAttributes)
        {
            var errorMessage = GetErrorMessage(validationAttribute, displayName);
            AddAttributes(htmlAttributes, validationAttribute, errorMessage);
        }

        if (htmlAttributes.Count == 0)
        {
            return null;
        }

        htmlAttributes.TryAdd("data-val", "true");
        return htmlAttributes;
    }

    /// <summary>
    /// Maps a <see cref="ValidationAttribute"/> to the corresponding <c>data-val-*</c> HTML attributes.
    /// Each supported attribute type has a specific mapping; custom attributes can implement
    /// <see cref="IClientValidationAdapter"/> to provide their own mappings.
    /// Uses <c>TryAdd</c> (first-wins) so that if multiple attributes emit the same key,
    /// the first one takes precedence.
    /// </summary>
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

            case MaxLengthAttribute maxla:
                htmlAttributes.TryAdd("data-val-maxlength", errorMessage);
                htmlAttributes.TryAdd("data-val-maxlength-max", maxla.Length.ToString(CultureInfo.InvariantCulture));
                break;

            case MinLengthAttribute minla:
                htmlAttributes.TryAdd("data-val-minlength", errorMessage);
                htmlAttributes.TryAdd("data-val-minlength-min", minla.Length.ToString(CultureInfo.InvariantCulture));
                break;

            case RangeAttribute ra:
                // Only emit client-side range attributes for numeric operand types.
                // The JS validator's range check is numeric (uses Number()), so non-numeric
                // ranges like RangeAttribute(typeof(DateTime), ...) have no client equivalent
                // and would emit unparseable values. Server-side validation still applies.
                if (!IsNumericRangeOperand(ra.OperandType))
                {
                    break;
                }
                // RangeAttribute lazily converts Minimum/Maximum from strings to OperandType.
                // Calling IsValid triggers SetupConversion(); same trick MVC's RangeAttributeAdapter uses.
                ra.IsValid(3);
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
                // The "*." prefix is a convention for the JS equalto validator to resolve the
                // other field relative to the current field's name prefix (e.g., "Model.Password").
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
                    foreach (var rule in adapter.GetClientValidationRules(errorMessage))
                    {
                        EmitRule(htmlAttributes, rule);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Serializes a <see cref="ClientValidationRule"/> into the flat <c>data-val-*</c> dictionary
    /// used during rendering. Uses <c>TryAdd</c> (first-wins) so existing entries are preserved.
    /// Non-string parameter values are formatted with invariant culture; <see langword="null"/>
    /// values are skipped.
    /// </summary>
    private static void EmitRule(Dictionary<string, object> htmlAttributes, ClientValidationRule rule)
    {
        htmlAttributes.TryAdd($"data-val-{rule.Name}", rule.ErrorMessage);
        foreach (var (paramName, paramValue) in rule.Parameters)
        {
            if (paramValue is null)
            {
                continue;
            }

            var formatted = paramValue switch
            {
                string s => s,
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(format: null, CultureInfo.InvariantCulture),
                _ => paramValue.ToString() ?? string.Empty,
            };

            htmlAttributes.TryAdd($"data-val-{rule.Name}-{paramName}", formatted);
        }
    }

    /// <summary>
    /// Returns true if the type is one the JS range validator can compare numerically.
    /// Excludes <see cref="DateTime"/> and other non-numeric operand types that
    /// <see cref="RangeAttribute"/> supports server-side but the client cannot.
    /// </summary>
    private static bool IsNumericRangeOperand(Type operandType)
        => operandType == typeof(int)
        || operandType == typeof(long)
        || operandType == typeof(short)
        || operandType == typeof(byte)
        || operandType == typeof(uint)
        || operandType == typeof(ulong)
        || operandType == typeof(ushort)
        || operandType == typeof(sbyte)
        || operandType == typeof(double)
        || operandType == typeof(float)
        || operandType == typeof(decimal);

    private static string? GetDisplayName(PropertyInfo property)
    {
        // TODO: Integrate localization
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();

        if (displayAttribute is not null)
        {
            return displayAttribute.GetName();
        }

        var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();

        if (displayNameAttribute is not null)
        {
            return displayNameAttribute.DisplayName;
        }

        return null;
    }

    private static string GetErrorMessage(ValidationAttribute validationAttribute, string displayName)
    {
        // TODO: Integrate localization
        return validationAttribute.FormatErrorMessage(displayName);

    }
}
