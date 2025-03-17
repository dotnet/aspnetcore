// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

internal sealed class RuntimeValidatableParameterInfoResolver : IValidatableInfoResolver
{
    // TODO: the implementation currently relies on static discovery of types.
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        validatableInfo = null;
        return false;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        if (parameterInfo.Name == null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameterInfo.ParameterType}' without a name. Parameters must have a name.");
        }

        var validationAttributes = parameterInfo
            .GetCustomAttributes<ValidationAttribute>()
            .ToArray();

        // If there are no validation attributes and this type is not a complex type
        // we don't need to validate it. Complex types without attributes are still
        // validatable because we want to run the validations on the properties.
        if (validationAttributes.Length == 0 && !IsClass(parameterInfo.ParameterType))
        {
            validatableInfo = null;
            return false;
        }
        validatableInfo = new RuntimeValidatableParameterInfo(
            parameterType: parameterInfo.ParameterType,
            name: parameterInfo.Name,
            displayName: GetDisplayName(parameterInfo),
            validationAttributes: validationAttributes
        );
        return true;
    }

    private static string GetDisplayName(ParameterInfo parameterInfo)
    {
        var displayAttribute = parameterInfo.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute != null)
        {
            return displayAttribute.Name ?? parameterInfo.Name!;
        }

        return parameterInfo.Name!;
    }

    internal sealed class RuntimeValidatableParameterInfo(
        Type parameterType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes) :
            ValidatableParameterInfo(parameterType, name, displayName)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;
    }

    private static bool IsClass(Type type)
    {
        // Skip primitives, enums, and common built-in types that don't need validation
        // if they don't have attributes
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeOnly) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid))
        {
            return false;
        }

        // Check if the underlying type in a nullable is valid
        if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            return IsClass(nullableType);
        }

        return type.IsClass;
    }
}
