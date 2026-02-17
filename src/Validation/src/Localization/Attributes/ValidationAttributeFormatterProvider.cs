// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

public class ValidationAttributeFormatterProvider : IValidationAttributeFormatterProvider
{
    public virtual IValidationAttributeFormatter GetFormatter(ValidationAttribute attribute)
    {
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
