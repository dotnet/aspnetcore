// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

internal class RuntimeValidatableParameterInfoResolver : IValidatableInfoResolver
{
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        validatableInfo = null;
        return false;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        Debug.Assert(parameterInfo.Name != null, "Parameter must have name");
        var validationAttributes = parameterInfo
            .GetCustomAttributes<ValidationAttribute>()
            .ToArray();
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
