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
            validationAttributes: validationAttributes
        );
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

    public ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
    {
        return null;
    }

    private class RuntimeValidatableParameterInfo(
        Type parameterType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes) :
            ValidatableParameterInfo(parameterType, name, displayName)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;
    }
}
