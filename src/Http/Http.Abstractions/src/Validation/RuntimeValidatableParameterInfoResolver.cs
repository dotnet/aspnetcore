// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

internal class RuntimeValidatableParameterInfoResolver : IValidatableInfoResolver
{
    public ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo)
    {
        Debug.Assert(parameterInfo.Name != null, "Parameter must have name");
        var validationAttributes = parameterInfo
            .GetCustomAttributes<ValidationAttribute>()
            .ToArray();
        return new RuntimeValidatableParameterInfo(
            parameterType: parameterInfo.ParameterType,
            name: parameterInfo.Name,
            displayName: GetDisplayName(parameterInfo),
            isNullable: IsNullable(parameterInfo),
            isRequired: validationAttributes.Any(a => a is RequiredAttribute),
            isEnumerable: IsEnumerable(parameterInfo),
            validationAttributes: validationAttributes
        );
    }

    private static bool IsNullable(ParameterInfo parameterInfo)
    {
        if (parameterInfo.ParameterType.IsValueType)
        {
            return false;
        }

        if (parameterInfo.ParameterType.IsGenericType &&
            parameterInfo.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return true;
        }

        return false;
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

    private static bool IsEnumerable(ParameterInfo parameterInfo)
    {
        if (parameterInfo.ParameterType.IsGenericType &&
            parameterInfo.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return true;
        }

        if (parameterInfo.ParameterType.IsArray)
        {
            return true;
        }

        return false;
    }

    public ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
    {
        return null;
    }

    private class RuntimeValidatableParameterInfo(
        Type parameterType,
        string name,
        string displayName,
        bool isNullable,
        bool isRequired,
        bool isEnumerable,
        ValidationAttribute[] validationAttributes) :
            ValidatableParameterInfo(parameterType, name, displayName, isNullable, isRequired, isEnumerable)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;
    }
}
