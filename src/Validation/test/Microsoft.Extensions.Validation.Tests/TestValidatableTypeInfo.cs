#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Tests;

internal class TestValidatableTypeInfo(
    Type type,
    ValidatablePropertyInfo[] members,
    ValidationAttribute[]? validationAttributes = default,
    DisplayAttribute? displayAttribute = null) : ValidatableTypeInfo(type, members)
{
    protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    protected override DisplayAttribute? GetDisplayAttribute() => _displayAttribute;

    private readonly ValidationAttribute[] _validationAttributes = validationAttributes ?? [];
    private readonly DisplayAttribute? _displayAttribute = displayAttribute;
}
