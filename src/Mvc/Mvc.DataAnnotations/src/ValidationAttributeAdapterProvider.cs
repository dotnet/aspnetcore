// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// Creates an <see cref="IAttributeAdapter"/> for the given attribute.
/// </summary>
public class ValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
{
    /// <summary>
    /// Creates an <see cref="IAttributeAdapter"/> for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to create an adapter for.</param>
    /// <param name="stringLocalizer">The localizer to provide to the adapter.</param>
    /// <returns>An <see cref="IAttributeAdapter"/> for the given attribute.</returns>
    public IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var type = attribute.GetType();

        if (typeof(RegularExpressionAttribute).IsAssignableFrom(type))
        {
            return new RegularExpressionAttributeAdapter((RegularExpressionAttribute)attribute, stringLocalizer);
        }
        else if (typeof(MaxLengthAttribute).IsAssignableFrom(type))
        {
            return new MaxLengthAttributeAdapter((MaxLengthAttribute)attribute, stringLocalizer);
        }
        else if (typeof(RequiredAttribute).IsAssignableFrom(type))
        {
            return new RequiredAttributeAdapter((RequiredAttribute)attribute, stringLocalizer);
        }
        else if (typeof(CompareAttribute).IsAssignableFrom(type))
        {
            return new CompareAttributeAdapter((CompareAttribute)attribute, stringLocalizer);
        }
        else if (typeof(MinLengthAttribute).IsAssignableFrom(type))
        {
            return new MinLengthAttributeAdapter((MinLengthAttribute)attribute, stringLocalizer);
        }
        else if (typeof(CreditCardAttribute).IsAssignableFrom(type))
        {
            return new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-creditcard", stringLocalizer);
        }
        else if (typeof(StringLengthAttribute).IsAssignableFrom(type))
        {
            return new StringLengthAttributeAdapter((StringLengthAttribute)attribute, stringLocalizer);
        }
        else if (typeof(RangeAttribute).IsAssignableFrom(type))
        {
            return new RangeAttributeAdapter((RangeAttribute)attribute, stringLocalizer);
        }
        else if (typeof(EmailAddressAttribute).IsAssignableFrom(type))
        {
            return new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-email", stringLocalizer);
        }
        else if (typeof(PhoneAttribute).IsAssignableFrom(type))
        {
            return new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-phone", stringLocalizer);
        }
        else if (typeof(UrlAttribute).IsAssignableFrom(type))
        {
            return new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-url", stringLocalizer);
        }
        else if (typeof(FileExtensionsAttribute).IsAssignableFrom(type))
        {
            return new FileExtensionsAttributeAdapter((FileExtensionsAttribute)attribute, stringLocalizer);
        }
        else
        {
            return null;
        }
    }
};
