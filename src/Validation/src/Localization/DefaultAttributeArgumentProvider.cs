// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Default implementation of <see cref="IAttributeArgumentProvider"/> that provides
/// format arguments for built-in <see cref="ValidationAttribute"/> types.
/// </summary>
public class DefaultAttributeArgumentProvider : IAttributeArgumentProvider
{
    /// <inheritdoc />
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
            // Other built-in attributes only use the display name.
            _ => [displayName],
        };
    }
}
