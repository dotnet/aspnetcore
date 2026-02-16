// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation.Localization;

namespace ConsoleValidationSample.Validators;

/// <summary>
/// Extends <see cref="DefaultAttributeArgumentProvider"/> to supply additional format arguments
/// for custom validation attributes such as <see cref="BannedCustomerAttribute"/>.
/// </summary>
internal class CustomAttributeArgumentProvider : DefaultAttributeArgumentProvider
{
    /// <inheritdoc />
    public override object?[] GetFormatArgs(ValidationAttribute attribute, string displayName)
    {
        return attribute switch
        {
            BannedCustomerAttribute bc => [displayName, bc.BannedName],
            _ => base.GetFormatArgs(attribute, displayName)
        };
    }
}
