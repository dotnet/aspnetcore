// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

/// <summary>
/// Walks the form's model type for <see cref="ValidationAttribute"/>s and builds a typed
/// <see cref="ClientValidationFormDescriptor"/> describing client-side validation rules.
/// Walks only the immediate (top-level) properties of the model type; nested complex
/// properties are not recursed into.
/// </summary>
internal sealed class EndpointClientValidationProvider : ClientValidationProvider
{
    // Stores culture-independent reflection results. Display names and error messages are
    // resolved per call so the output respects CultureInfo.CurrentUICulture.
    private readonly ConcurrentDictionary<Type, PropertyMetadata[]> _metadataCache = new();

    private readonly IValidationLocalizer? _validationLocalizer;

    [UnconditionalSuppressMessage("Trimming", "IL2066",
        Justification = "Preserves ValidationOptions's parameterless constructor used by Microsoft.Extensions.Options to materialize IOptions<ValidationOptions>.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ValidationOptions))]
    public EndpointClientValidationProvider(IServiceProvider serviceProvider)
    {
        _validationLocalizer = serviceProvider.GetService<IOptions<ValidationOptions>>()?.Value?.Localizer;
    }

    public override ClientValidationFormDescriptor? GetFormDescriptor(EditContext editContext)
    {
        ArgumentNullException.ThrowIfNull(editContext);

        var modelType = editContext.Model.GetType();
        var properties = _metadataCache.GetOrAdd(modelType, static type => BuildMetadata(type));

        if (properties.Length == 0)
        {
            return null;
        }

        var fields = new List<ClientValidationFieldDescriptor>(properties.Length);

        foreach (var property in properties)
        {
            var displayName = ResolveDisplayName(in property);
            var rules = new List<ClientValidationRule>();

            foreach (var attribute in property.ValidationAttributes)
            {
                var errorMessage = ResolveErrorMessage(attribute, property.Name, displayName, property.DeclaringType);
                AddRules(rules, attribute, errorMessage);
            }

            // Skip properties whose attributes produced no client-renderable rules
            // (e.g. RangeAttribute with a non-numeric operand type).
            if (rules.Count > 0)
            {
                fields.Add(new ClientValidationFieldDescriptor(property.Name, rules));
            }
        }

        return fields.Count == 0 ? null : new ClientValidationFormDescriptor(fields);
    }

    // Maps each ValidationAttribute to one or more ClientValidationRule entries. Custom
    // attributes that implement IClientValidationAdapter contribute their own rules.
    private static void AddRules(
        List<ClientValidationRule> rules,
        ValidationAttribute validationAttribute,
        string errorMessage)
    {
        switch (validationAttribute)
        {
            case RequiredAttribute:
                rules.Add(new ClientValidationRule("required", errorMessage));
                break;

            case StringLengthAttribute sla:
                {
                    var hasMax = sla.MaximumLength != int.MaxValue;
                    var hasMin = sla.MinimumLength != 0;
                    var parameters = (hasMax, hasMin) switch
                    {
                        (true, true) => new Dictionary<string, string>
                        {
                            ["max"] = sla.MaximumLength.ToString(CultureInfo.InvariantCulture),
                            ["min"] = sla.MinimumLength.ToString(CultureInfo.InvariantCulture),
                        },
                        (true, false) => new Dictionary<string, string>
                        {
                            ["max"] = sla.MaximumLength.ToString(CultureInfo.InvariantCulture),
                        },
                        (false, true) => new Dictionary<string, string>
                        {
                            ["min"] = sla.MinimumLength.ToString(CultureInfo.InvariantCulture),
                        },
                        _ => null,
                    };
                    rules.Add(new ClientValidationRule("length", errorMessage, parameters));
                    break;
                }

            case MaxLengthAttribute maxla:
                rules.Add(new ClientValidationRule("maxlength", errorMessage,
                    new Dictionary<string, string>
                    {
                        ["max"] = maxla.Length.ToString(CultureInfo.InvariantCulture),
                    }));
                break;

            case MinLengthAttribute minla:
                rules.Add(new ClientValidationRule("minlength", errorMessage,
                    new Dictionary<string, string>
                    {
                        ["min"] = minla.Length.ToString(CultureInfo.InvariantCulture),
                    }));
                break;

            case RangeAttribute ra:
                {
                    // The JS range validator is numeric-only (uses Number()); skip non-numeric operands.
                    if (!IsNumericRangeOperand(ra.OperandType))
                    {
                        break;
                    }
                    // Triggers RangeAttribute.SetupConversion() to convert string Min/Max to OperandType.
                    ra.IsValid(3);
                    rules.Add(new ClientValidationRule("range", errorMessage,
                        new Dictionary<string, string>
                        {
                            ["min"] = Convert.ToString(ra.Minimum, CultureInfo.InvariantCulture)!,
                            ["max"] = Convert.ToString(ra.Maximum, CultureInfo.InvariantCulture)!,
                        }));
                    break;
                }

            case RegularExpressionAttribute rea:
                rules.Add(new ClientValidationRule("regex", errorMessage,
                    new Dictionary<string, string>
                    {
                        ["pattern"] = rea.Pattern,
                    }));
                break;

            case CompareAttribute ca:
                rules.Add(new ClientValidationRule("equalto", errorMessage,
                    new Dictionary<string, string>
                    {
                        // "*." prefix tells the JS equalto validator to resolve the other field
                        // relative to the current field's name prefix.
                        ["other"] = "*." + ca.OtherProperty,
                    }));
                break;

            case EmailAddressAttribute:
                rules.Add(new ClientValidationRule("email", errorMessage));
                break;

            case UrlAttribute:
                rules.Add(new ClientValidationRule("url", errorMessage));
                break;

            case PhoneAttribute:
                rules.Add(new ClientValidationRule("phone", errorMessage));
                break;

            case CreditCardAttribute:
                rules.Add(new ClientValidationRule("creditcard", errorMessage));
                break;

            case FileExtensionsAttribute fea:
                {
                    var normalizedExtensions = fea.Extensions
                        .Replace(" ", string.Empty)
                        .Replace(".", string.Empty)
                        .ToLowerInvariant();
                    var parsedExtensions = string.Join(",", normalizedExtensions.Split(',').Select(e => "." + e));
                    rules.Add(new ClientValidationRule("fileextensions", errorMessage,
                        new Dictionary<string, string>
                        {
                            ["extensions"] = parsedExtensions,
                        }));
                    break;
                }

            default:
                if (validationAttribute is IClientValidationAdapter adapter)
                {
                    foreach (var rule in adapter.GetClientValidationRules(errorMessage))
                    {
                        rules.Add(rule);
                    }
                }
                break;
        }
    }

    // Mirrors the decision tree used by the server-side validation.
    // Resource-attribute display names bypass the localizer (resource lookup is the canonical
    // localized source). Literal display names act as both lookup key and fallback for the localizer.
    private string ResolveDisplayName(in PropertyMetadata metadata)
    {
        if (metadata.ResourceDisplayAttribute is { } resourceAttribute)
        {
            return resourceAttribute.GetName() ?? metadata.Name;
        }

        if (metadata.LiteralDisplayName is not { } literal)
        {
            return metadata.Name;
        }

        if (_validationLocalizer is null)
        {
            return literal;
        }

        return _validationLocalizer.ResolveDisplayName(new DisplayNameLocalizationContext
        {
            DeclaringType = metadata.DeclaringType,
            DisplayName = literal,
            MemberName = metadata.Name,
        }) ?? literal;
    }

    // Mirrors the decision tree used by the server-side validation. Falls back to
    // FormatErrorMessage when no localizer is configured or the attribute already supplies
    // resource-based localization.
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

    // RangeAttribute supports non-numeric operand types (e.g., DateTime) that the JS validator
    // can't compare; only emit "range" rules for numeric operand types.
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

    // Per-property reflection results. Culture-independent; localized text is resolved per call.
    // At most one of ResourceDisplayAttribute and LiteralDisplayName is non-null; both null means
    // the property has no display attribute.
    private readonly struct PropertyMetadata(
        string name,
        ValidationAttribute[] validationAttributes,
        Type? declaringType,
        DisplayAttribute? resourceDisplayAttribute,
        string? literalDisplayName)
    {
        public string Name { get; } = name;

        public ValidationAttribute[] ValidationAttributes { get; } = validationAttributes;

        public Type? DeclaringType { get; } = declaringType;

        // [Display(Name=..., ResourceType=...)]
        public DisplayAttribute? ResourceDisplayAttribute { get; } = resourceDisplayAttribute;

        // [Display(Name="X")] (no ResourceType) or [DisplayName("X")].
        public string? LiteralDisplayName { get; } = literalDisplayName;
    }

    // Walks the model type's public instance properties and returns metadata for those with at
    // least one ValidationAttribute. Top-level only - does NOT recurse into nested complex types.
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Model types are application code and are preserved by default.")]
    private static PropertyMetadata[] BuildMetadata(Type modelType)
    {
        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0)
        {
            return [];
        }

        var result = new List<PropertyMetadata>(properties.Length);
        foreach (var property in properties)
        {
            var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();
            if (validationAttributes.Length == 0)
            {
                continue;
            }

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

            result.Add(new PropertyMetadata(
                name: property.Name,
                validationAttributes,
                declaringType: property.DeclaringType,
                resourceDisplayAttribute,
                literalDisplayName));
        }

        return result.ToArray();
    }
}
