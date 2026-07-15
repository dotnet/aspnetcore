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

internal sealed class DataAnnotationsClientValidationProvider : ClientValidationProvider
{
    private readonly ClientValidationCache _clientValidationCache;
    private readonly IValidationLocalizer? _validationLocalizer;

    [UnconditionalSuppressMessage("Trimming", "IL2066", Justification = "Preserves ValidationOptions's parameterless constructor used by Microsoft.Extensions.Options to materialize IOptions<ValidationOptions>.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ValidationOptions))]
    public DataAnnotationsClientValidationProvider(ClientValidationCache clientValidationCache, IOptions<ValidationOptions> validationOptions)
    {
        _clientValidationCache = clientValidationCache;
        _validationLocalizer = validationOptions.Value.Localizer;
    }

    public override RenderFragment? RenderClientValidationRules(EditContext editContext, IReadOnlyDictionary<FieldIdentifier, string> renderedFields)
    {
        var json = SerializeClientValidationData(editContext, renderedFields);
        if (json is null)
        {
            return null;
        }

        return builder =>
        {
            builder.OpenElement(0, "blazor-client-validation-data");
            // The rules are carried in the data-rules attribute, not as element content, so the
            // element renders nothing at all in the DOM.
            builder.AddAttribute(1, "data-rules", json);
            builder.CloseElement();
        };
    }

    // Returns JSON string with the client-validation payload for the rendered fields directly, or null when
    // there is nothing to emit.
    internal string? SerializeClientValidationData(EditContext editContext, IReadOnlyDictionary<FieldIdentifier, string> renderedFields)
    {
        ArgumentNullException.ThrowIfNull(editContext);

        if (renderedFields.Count == 0)
        {
            return null;
        }

        var writer = new ClientValidationDataWriter();
        var validatableFields = _clientValidationCache.GetValidatableFieldMetadata(renderedFields, editContext.Model);

        foreach (var (renderedName, fieldMetadata) in validatableFields)
        {
            WriteFieldRules(writer, renderedName, fieldMetadata);
        }

        return writer.Complete();
    }

    private void WriteFieldRules(ClientValidationDataWriter writer, string renderedName, ClientValidationFieldMetadata fieldMetadata)
    {
        var displayName = ResolveDisplayName(fieldMetadata);
        writer.BeginField(renderedName);

        foreach (var attribute in fieldMetadata.ValidationAttributes)
        {
            var errorMessage = ResolveErrorMessage(attribute, fieldMetadata.PropertyName, displayName, fieldMetadata.DeclaringType);

            if (TryWriteBuiltInRule(writer, attribute, errorMessage))
            {
                continue;
            }

            if (attribute is IClientValidationAdapter adapter)
            {
                foreach (var customRule in adapter.GetClientValidationRules())
                {
                    writer.BeginRule(customRule.Name, errorMessage);
                    if (customRule.Parameters is { Count: > 0 } parameters)
                    {
                        foreach (var kvp in parameters)
                        {
                            writer.Param(kvp.Key, kvp.Value);
                        }
                    }
                    writer.EndRule();
                }
            }
        }

        writer.EndField();
    }

    // Writes the rule for each built-in ValidationAttribute directly to JSON and returns true, or
    // returns false when the attribute is not a built-in (custom adapters handle those elsewhere).
    private static bool TryWriteBuiltInRule(ClientValidationDataWriter writer, ValidationAttribute validationAttribute, string errorMessage)
    {
        switch (validationAttribute)
        {
            case RequiredAttribute:
                writer.BeginRule("required"u8, errorMessage);
                writer.EndRule();
                return true;

            case StringLengthAttribute sla:
                writer.BeginRule("length"u8, errorMessage);
                if (sla.MaximumLength != int.MaxValue)
                {
                    writer.Param("max"u8, sla.MaximumLength);
                }
                if (sla.MinimumLength != 0)
                {
                    writer.Param("min"u8, sla.MinimumLength);
                }
                writer.EndRule();
                return true;

            case MaxLengthAttribute maxla:
                writer.BeginRule("maxlength"u8, errorMessage);
                writer.Param("max"u8, maxla.Length);
                writer.EndRule();
                return true;

            case MinLengthAttribute minla:
                writer.BeginRule("minlength"u8, errorMessage);
                writer.Param("min"u8, minla.Length);
                writer.EndRule();
                return true;

            // The JS range validator is numeric-only (uses Number()); skip non-numeric operands.
            case RangeAttribute ra when IsNumericRangeOperand(ra.OperandType):
                // Triggers RangeAttribute.SetupConversion() to convert string Min/Max to OperandType.
                ra.IsValid(3);
                writer.BeginRule("range"u8, errorMessage);
                writer.Param("min"u8, Convert.ToString(ra.Minimum, CultureInfo.InvariantCulture)!);
                writer.Param("max"u8, Convert.ToString(ra.Maximum, CultureInfo.InvariantCulture)!);
                writer.EndRule();
                return true;

            case RegularExpressionAttribute rea:
                writer.BeginRule("regex"u8, errorMessage);
                writer.Param("pattern"u8, rea.Pattern);
                writer.EndRule();
                return true;

            case CompareAttribute ca:
                writer.BeginRule("equalto"u8, errorMessage);
                // "*." prefix tells the JS equalto validator to resolve the other field relative to
                // the current field's name prefix.
                writer.Param("other"u8, "*." + ca.OtherProperty);
                writer.EndRule();
                return true;

            case EmailAddressAttribute:
                writer.BeginRule("email"u8, errorMessage);
                writer.EndRule();
                return true;

            case UrlAttribute:
                writer.BeginRule("url"u8, errorMessage);
                writer.EndRule();
                return true;

            case PhoneAttribute:
                writer.BeginRule("phone"u8, errorMessage);
                writer.EndRule();
                return true;

            case CreditCardAttribute:
                writer.BeginRule("creditcard"u8, errorMessage);
                writer.EndRule();
                return true;

            case FileExtensionsAttribute fea:
                writer.BeginRule("fileextensions"u8, errorMessage);
                writer.Param("extensions"u8, GetNormalizedExtensions(fea));
                writer.EndRule();
                return true;

            default:
                return false;
        }
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
