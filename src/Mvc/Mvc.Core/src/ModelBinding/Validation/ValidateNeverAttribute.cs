// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Indicates that a property or parameter should be excluded from validation.
/// When applied to a property, the validation system excludes that property.
/// When applied to a parameter, the validation system excludes that parameter.
/// When applied to a type, the validation system excludes all properties within that type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class ValidateNeverAttribute : Attribute, IPropertyValidationFilter
{
    /// <inheritdoc />
    public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry)
    {
        return false;
    }
}
