// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

/// <summary>
/// Iterates the inputs registered on the <see cref="EditContext"/> and builds a typed <see cref="ClientValidationFormDescriptor"/>
/// describing client-side validation rules.
/// Emits client-side validation rules only for fields that would be validated on the server as well.
/// </summary>
internal sealed class EndpointClientValidationProvider : ClientValidationProvider
{
    private readonly ClientValidationCache _clientValidationCache;
    private readonly IValidationLocalizer? _validationLocalizer;

    [UnconditionalSuppressMessage("Trimming", "IL2066", Justification = "Preserves ValidationOptions's parameterless constructor used by Microsoft.Extensions.Options to materialize IOptions<ValidationOptions>.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ValidationOptions))]
    public EndpointClientValidationProvider(ClientValidationCache clientValidationCache, IOptions<ValidationOptions> validationOptions)
    {
        _clientValidationCache = clientValidationCache;
        _validationLocalizer = validationOptions.Value.Localizer;
    }

    public override ClientValidationFormDescriptor? GetFormDescriptor(EditContext editContext, IReadOnlyDictionary<FieldIdentifier, string> renderedFields)
    {
        ArgumentNullException.ThrowIfNull(editContext);

        if (renderedFields.Count == 0)
        {
            return null;
        }

        List<ClientValidationFieldDescriptor>? fieldDescriptors = null;
        var validatableFields = _clientValidationCache.GetValidatableFieldMetadata(renderedFields, editContext.Model);

        foreach (var (renderedName, fieldMetadata) in validatableFields)
        {
            if (BuildFieldDescriptor(renderedName, fieldMetadata) is { } fieldDescriptor)
            {
                (fieldDescriptors ??= []).Add(fieldDescriptor);
            }
        }

        return fieldDescriptors is { Count: > 0 }
           ? new ClientValidationFormDescriptor(fieldDescriptors)
           : null;
    }

    private ClientValidationFieldDescriptor? BuildFieldDescriptor(string renderedName, ClientValidationFieldMetadata fieldMetadata)
    {
        var displayName = ResolveDisplayName(fieldMetadata);
        var rules = new List<ClientValidationRule>();

        foreach (var attribute in fieldMetadata.ValidationAttributes)
        {
            var errorMessage = ResolveErrorMessage(attribute, fieldMetadata.PropertyName, displayName, fieldMetadata.DeclaringType);

            if (GetBuiltInValidationRule(attribute, errorMessage) is { } rule)
            {
                rules.Add(rule);
            }
            else if (attribute is IClientValidationAdapter adapter)
            {
                foreach (var customRule in adapter.GetClientValidationRules(errorMessage))
                {
                    rules.Add(customRule);
                }
            }
        }

        return rules.Count > 0
            ? new ClientValidationFieldDescriptor(renderedName, rules)
            : null;
    }

    // Maps each built-in ValidationAttribute to its single ClientValidationRule. Custom
    // attributes that implement IClientValidationAdapter contribute their own rules elsewhere.
    private static ClientValidationRule? GetBuiltInValidationRule(ValidationAttribute validationAttribute, string errorMessage)
    {
        return validationAttribute switch
        {
            RequiredAttribute => new ClientValidationRule("required", errorMessage),
            StringLengthAttribute sla => new ClientValidationRule("length", errorMessage, GetStringLengthParameters(sla)),
            MaxLengthAttribute maxla => new ClientValidationRule("maxlength", errorMessage,
                new Dictionary<string, string>
                {
                    ["max"] = maxla.Length.ToString(CultureInfo.InvariantCulture),
                }),
            MinLengthAttribute minla => new ClientValidationRule("minlength", errorMessage,
                new Dictionary<string, string>
                {
                    ["min"] = minla.Length.ToString(CultureInfo.InvariantCulture),
                }),
            // The JS range validator is numeric-only (uses Number()); skip non-numeric operands.
            RangeAttribute ra when IsNumericRangeOperand(ra.OperandType) => GetRangeRule(ra, errorMessage),
            RegularExpressionAttribute rea => new ClientValidationRule("regex", errorMessage,
                new Dictionary<string, string>
                {
                    ["pattern"] = rea.Pattern,
                }),
            CompareAttribute ca => new ClientValidationRule("equalto", errorMessage,
                new Dictionary<string, string>
                {
                    // "*." prefix tells the JS equalto validator to resolve the other field
                    // relative to the current field's name prefix.
                    ["other"] = "*." + ca.OtherProperty,
                }),
            EmailAddressAttribute => new ClientValidationRule("email", errorMessage),
            UrlAttribute => new ClientValidationRule("url", errorMessage),
            PhoneAttribute => new ClientValidationRule("phone", errorMessage),
            CreditCardAttribute => new ClientValidationRule("creditcard", errorMessage),
            FileExtensionsAttribute fea => new ClientValidationRule("fileextensions", errorMessage,
                new Dictionary<string, string>
                {
                    ["extensions"] = GetNormalizedExtensions(fea),
                }),
            _ => null,
        };
    }

    private static Dictionary<string, string>? GetStringLengthParameters(StringLengthAttribute sla)
    {
        var hasMax = sla.MaximumLength != int.MaxValue;
        var hasMin = sla.MinimumLength != 0;
        return (hasMax, hasMin) switch
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
    }

    private static ClientValidationRule GetRangeRule(RangeAttribute ra, string errorMessage)
    {
        // Triggers RangeAttribute.SetupConversion() to convert string Min/Max to OperandType.
        ra.IsValid(3);
        return new ClientValidationRule("range", errorMessage,
            new Dictionary<string, string>
            {
                ["min"] = Convert.ToString(ra.Minimum, CultureInfo.InvariantCulture)!,
                ["max"] = Convert.ToString(ra.Maximum, CultureInfo.InvariantCulture)!,
            });
    }

    private static string GetNormalizedExtensions(FileExtensionsAttribute fea)
    {
        var normalizedExtensions = fea.Extensions
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .ToLowerInvariant();
        return string.Join(",", normalizedExtensions.Split(',').Select(e => "." + e));
    }

    // Mirrors the decision tree used by the server-side validation.
    // Resource-attribute display names bypass the localizer (resource lookup is the canonical
    // localized source). Literal display names act as both lookup key and fallback for the localizer.
    private string ResolveDisplayName(in ClientValidationFieldMetadata metadata)
    {
        if (metadata.ResourceDisplayAttribute is { } resourceAttribute)
        {
            return resourceAttribute.GetName() ?? metadata.PropertyName;
        }

        if (metadata.LiteralDisplayName is not { } literal)
        {
            return metadata.PropertyName;
        }

        if (_validationLocalizer is null)
        {
            return literal;
        }

        return _validationLocalizer.ResolveDisplayName(new DisplayNameLocalizationContext
        {
            Type = metadata.DeclaringType,
            DisplayName = literal,
            MemberName = metadata.PropertyName,
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
}
