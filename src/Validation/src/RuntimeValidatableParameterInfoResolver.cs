#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace Microsoft.Extensions.Validation;

internal sealed class RuntimeValidatableParameterInfoResolver : IValidatableInfoResolver
{
    // TODO: the implementation currently relies on static discovery of types.
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableTypeInfo)
    {
        validatableTypeInfo = null;
        return false;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableParameterInfo)
    {
        if (parameterInfo.Name == null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameterInfo.ParameterType}' without a name. Parameters must have a name.");
        }

        // Skip method parameter if it or its type are annotated with SkipValidationAttribute.
        if (parameterInfo.GetCustomAttribute<SkipValidationAttribute>() != null ||
            parameterInfo.ParameterType.GetCustomAttribute<SkipValidationAttribute>() != null)
        {
            validatableParameterInfo = null;
            return false;
        }

        var validationAttributes = parameterInfo
            .GetCustomAttributes<ValidationAttribute>()
            .ToArray();

        // If there are no validation attributes and this type is not a complex type
        // we don't need to validate it. Complex types without attributes are still
        // validatable because we want to run the validations on the properties.
        if (validationAttributes.Length == 0 && !IsComplexType(parameterInfo.ParameterType))
        {
            validatableParameterInfo = null;
            return false;
        }

        var displayNameInfo = ResolveDisplayInfo(parameterInfo);

        validatableParameterInfo = new RuntimeValidatableParameterInfo(
            parameterType: parameterInfo.ParameterType,
            name: parameterInfo.Name,
            displayNameInfo: displayNameInfo,
            validationAttributes: validationAttributes
        );
        return true;
    }

    private static DisplayNameInfo? ResolveDisplayInfo(ParameterInfo parameterInfo)
    {
        var displayAttribute = parameterInfo.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute is { ResourceType: not null, Name: not null })
        {
            // Resource-based display name from [Display(ResourceType = ..., Name = ...)] is the
            // canonical localized source; the IValidationLocalizer is intentionally bypassed.
            // The DisplayAttribute instance is retained for the lifetime of the resolver, mirroring
            // the source-generator's static accessor design.
            return new ParameterReflectionDisplayName(displayAttribute);
        }

        if (displayAttribute?.Name is not null)
        {
            // Literal name from [Display(Name = "...")].
            return new LiteralDisplayName(displayAttribute.Name);
        }

        var displayNameAttribute = parameterInfo.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttribute is not null)
        {
            // Literal name from [DisplayName("...")].
            return new LiteralDisplayName(displayNameAttribute.DisplayName);
        }

        return null;
    }

    internal sealed class RuntimeValidatableParameterInfo(
        Type parameterType,
        string name,
        DisplayNameInfo? displayNameInfo,
        ValidationAttribute[] validationAttributes) :
            ValidatableParameterInfo(parameterType, name, displayNameInfo)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;
    }

    private sealed class LiteralDisplayName(string literal) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? type)
        {
            var localizer = context.ValidationOptions.Localizer;
            if (localizer is null)
            {
                return literal;
            }

            // The literal acts as both the lookup key for the localizer AND the fallback display
            // name when the localizer can't translate.
            return localizer.ResolveDisplayName(new DisplayNameLocalizationContext
            {
                Type = type,
                DisplayName = literal,
                MemberName = memberName,
            }) ?? literal;
        }
    }

    private sealed class ParameterReflectionDisplayName(DisplayAttribute attribute) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? type)
            => attribute.GetName();
    }

    private static bool IsComplexType(Type type)
    {
        // Skip primitives, enums, common built-in types, and types that are specially
        // handled by RDF/RDG that don't need validation if they don't have attributes
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeOnly) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid) ||
            type == typeof(ClaimsPrincipal) ||
            type == typeof(CancellationToken) ||
            type == typeof(Stream) ||
            type == typeof(PipeReader))
        {
            return false;
        }

        // Check if the underlying type in a nullable is valid
        if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            return IsComplexType(nullableType);
        }

        // Complex types include both reference types (classes) and value types (structs, record structs)
        // that aren't in the exclusion list above
        return type.IsClass || type.IsValueType;
    }
}
