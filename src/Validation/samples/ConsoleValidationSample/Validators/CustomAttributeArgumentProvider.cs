// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Validation.Localization;

namespace ConsoleValidationSample.Validators;

internal class CustomAttributeArgumentProvider : DefaultAttributeArgumentProvider
{
    public override object?[] GetFormatArgs(ValidationAttribute attribute, string displayName)
    {
        return attribute switch
        {
            BannedCustomerAttribute bc => [displayName, bc.BannedName],
            _ => base.GetFormatArgs(attribute, displayName)
        };
    }
}
