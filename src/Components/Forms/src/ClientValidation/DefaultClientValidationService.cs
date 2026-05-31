// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Generates <c>data-val-*</c> HTML attributes from <see cref="ValidationAttribute"/>s on model properties.
/// </summary>
internal sealed class DefaultClientValidationService : IClientValidationService
{
    // Stores only culture-independent reflection results. Display name and error message text
    // are resolved per call so the output respects CultureInfo.CurrentUICulture.
    private readonly ConcurrentDictionary<(Type ModelType, string FieldName), FieldMetadata> _metadataCache = new();

    private readonly IValidationLocalizer? _validationLocalizer;

    [UnconditionalSuppressMessage("Trimming", "IL2066",
        Justification = "DynamicDependency preserves ValidationOptions's parameterless constructor used by Microsoft.Extensions.Options to materialize IOptions<ValidationOptions>.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ValidationOptions))]
    public DefaultClientValidationService(IServiceProvider serviceProvider)
    {
        _validationLocalizer = serviceProvider.GetService<IOptions<ValidationOptions>>()?.Value?.Localizer;
    }

    public IReadOnlyDictionary<string, object>? GetClientValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        var modelType = fieldIdentifier.Model.GetType();
        var cacheKey = (modelType, fieldIdentifier.FieldName);
        var metadata = _metadataCache.GetOrAdd(cacheKey, static key => BuildMetadata(key.ModelType, key.FieldName));

        if (metadata.ValidationAttributes.Length == 0)
        {
            return null;
        }

        var displayName = ResolveDisplayName(in metadata, fieldIdentifier.FieldName);
        var htmlAttributes = new Dictionary<string, object>();

        foreach (var validationAttribute in metadata.ValidationAttributes)
        {
            var errorMessage = ResolveErrorMessage(validationAttribute, fieldIdentifier.FieldName, displayName, metadata.DeclaringType);
            AddAttributes(htmlAttributes, validationAttribute, errorMessage);
        }

        if (htmlAttributes.Count == 0)
        {
            return null;
        }

        htmlAttributes.TryAdd("data-val", "true");
        return htmlAttributes;
    }

    // Maps each ValidationAttribute to its data-val-* keys. Custom attributes can implement
    // IClientValidationAdapter for their own mappings. TryAdd is first-wins.
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
                // The JS range validator is numeric-only (uses Number()); skip non-numeric operands.
                if (!IsNumericRangeOperand(ra.OperandType))
                {
                    break;
                }
                // Triggers RangeAttribute.SetupConversion() to convert string Min/Max to OperandType.
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
                // "*." prefix tells the JS equalto validator to resolve the other field
                // relative to the current field's name prefix (e.g., "Model.Password").
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

    // Flattens a ClientValidationRule into the data-val-* dictionary. Null parameter values
    // are skipped; non-string values are formatted with invariant culture.
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

    // RangeAttribute supports non-numeric operand types (e.g., DateTime) that the JS validator
    // can't compare; only emit data-val-range for numeric ones.
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

    // Mirrors the decision tree used the server-side validation.
    // Resource attribute bypasses the localizer (resource lookup is the canonical localized source).
    // Literal acts as both lookup key and fallback for the localizer.
    private string ResolveDisplayName(in FieldMetadata metadata, string fieldName)
    {
        if (metadata.ResourceDisplayAttribute is { } resourceAttribute)
        {
            return resourceAttribute.GetName() ?? fieldName;
        }

        if (metadata.LiteralDisplayName is not { } literal)
        {
            return fieldName;
        }

        if (_validationLocalizer is null)
        {
            return literal;
        }

        return _validationLocalizer.ResolveDisplayName(new DisplayNameLocalizationContext
        {
            DeclaringType = metadata.DeclaringType,
            DisplayName = literal,
            MemberName = fieldName,
        }) ?? literal;
    }

    // Mirrors the decision tree used the server-side validation (see ResolveDisplayName).
    private string ResolveErrorMessage(
        ValidationAttribute attribute,
        string fieldName,
        string displayName,
        Type? declaringType)
    {
        if (_validationLocalizer is null || attribute.ErrorMessageResourceType is not null)
        {
            return attribute.FormatErrorMessage(displayName);
        }

        return _validationLocalizer.ResolveErrorMessage(new ErrorMessageLocalizationContext
        {
            MemberName = fieldName,
            DisplayName = displayName,
            DeclaringType = declaringType,
            Attribute = attribute,
        }) ?? attribute.FormatErrorMessage(displayName);
    }

    // All fields are culture-independent; localized text is resolved per call. At most one of
    // ResourceDisplayAttribute and LiteralDisplayName is non-null; both null means the property
    // has no display attribute.
    private readonly struct FieldMetadata(
        ValidationAttribute[] validationAttributes,
        Type? declaringType,
        DisplayAttribute? resourceDisplayAttribute,
        string? literalDisplayName)
    {
        // FieldMetadata.Empty is the sentinel for "no validatable property by this name".
        public static readonly FieldMetadata Empty = new(
            validationAttributes: [],
            declaringType: null,
            resourceDisplayAttribute: null,
            literalDisplayName: null);

        public ValidationAttribute[] ValidationAttributes { get; } = validationAttributes;

        public Type? DeclaringType { get; } = declaringType;

        // [Display(Name=..., ResourceType=...)]
        public DisplayAttribute? ResourceDisplayAttribute { get; } = resourceDisplayAttribute;

        // [Display(Name="X")] (no ResourceType) or [DisplayName("X")].
        public string? LiteralDisplayName { get; } = literalDisplayName;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Model types are application code and are preserved by default.")]
    private static FieldMetadata BuildMetadata(Type modelType, string fieldName)
    {
        var property = modelType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            return FieldMetadata.Empty;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();

        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>(inherit: true);
        DisplayAttribute? resourceDisplayAttribute = null;
        string? literalDisplayName = null;

        if (displayAttribute is { ResourceType: not null, Name: not null })
        {
            resourceDisplayAttribute = displayAttribute;
        }
        else
        {
            literalDisplayName = displayAttribute?.Name
                ?? property.GetCustomAttribute<DisplayNameAttribute>(inherit: true)?.DisplayName;
        }

        return new FieldMetadata(validationAttributes, property.DeclaringType, resourceDisplayAttribute, literalDisplayName);
    }
}
