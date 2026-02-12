// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

public class DefaultAttributeArgumentProvider : IAttributeArgumentProvider
{
    public virtual object?[] GetFormatArgs(ValidationAttribute attribute, string displayName)
    {
        return attribute switch
        {
            RangeAttribute range =>
                [displayName, range.Minimum, range.Maximum],
            StringLengthAttribute sl =>
                [displayName, sl.MaximumLength, sl.MinimumLength],
            MinLengthAttribute ml =>
                [displayName, ml.Length],
            MaxLengthAttribute ml =>
                [displayName, ml.Length],
            RegularExpressionAttribute re =>
                [displayName, re.Pattern],
            CompareAttribute cmp =>
                [displayName, cmp.OtherProperty],
            FileExtensionsAttribute fe =>
                [displayName, fe.Extensions],
            LengthAttribute la =>
                [displayName, la.MinimumLength, la.MaximumLength],
            // For most attributes (Required, EmailAddress, Phone, CreditCard, Url,
            // Base64String, AllowedValues, DeniedValues, etc.),
            // only the display name is used.
            _ => [displayName],
        };
    }
}
