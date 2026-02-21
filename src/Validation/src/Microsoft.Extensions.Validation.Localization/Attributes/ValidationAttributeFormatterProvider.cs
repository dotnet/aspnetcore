// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

/// <summary>
/// Default implementation of <see cref="IValidationAttributeFormatterProvider"/> that returns
/// <see cref="IValidationAttributeFormatter"/> instances for built-in <see cref="ValidationAttribute"/> types.
/// Derive from this class and override <see cref="GetFormatter"/> to add support for custom attributes.
/// See <see cref="IValidationAttributeFormatter"/> for detailed guidance and examples.
/// </summary>
public class ValidationAttributeFormatterProvider : IValidationAttributeFormatterProvider
{
    /// <summary>
    /// Returns an <see cref="IValidationAttributeFormatter"/> for the specified <paramref name="attribute"/>.
    /// If the attribute implements <see cref="IValidationAttributeFormatter"/> itself, it is returned directly.
    /// Otherwise, a built-in formatter is returned for known attribute types, or
    /// <see cref="DefaultAttributeFormatter.Instance"/> for attributes that only use the display name.
    /// </summary>
    /// <param name="attribute">The validation attribute to get a formatter for.</param>
    /// <returns>An <see cref="IValidationAttributeFormatter"/> capable of formatting the attribute's error message.</returns>
    public virtual IValidationAttributeFormatter GetFormatter(ValidationAttribute attribute)
    {
        if (attribute is IValidationAttributeFormatter selfFormatter)
        {
            return selfFormatter;
        }

        return attribute switch
        {
            RangeAttribute range => new RangeAttributeFormatter(range),
            MinLengthAttribute ml => new MinLengthAttributeFormatter(ml),
            MaxLengthAttribute ml => new MaxLengthAttributeFormatter(ml),
            LengthAttribute la => new LengthAttributeFormatter(la),
            StringLengthAttribute sl => new StringLengthAttributeFormatter(sl),
            RegularExpressionAttribute re => new RegularExpressionAttributeFormatter(re),
            FileExtensionsAttribute fe => new FileExtensionsAttributeFormatter(fe),
            CompareAttribute cmp => new CompareAttributeFormatter(cmp),
            // Other built-in attributes only use the display name.
            _ => DefaultAttributeFormatter.Instance
        };
    }
}
