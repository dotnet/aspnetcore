// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
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
        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            IAttributeAdapter adapter;

            var type = attribute.GetType();

            if (typeof(RegularExpressionAttribute).IsAssignableFrom(type))
            {
                adapter = new RegularExpressionAttributeAdapter((RegularExpressionAttribute)attribute, stringLocalizer);
            }
            else if (typeof(MaxLengthAttribute).IsAssignableFrom(type))
            {
                adapter = new MaxLengthAttributeAdapter((MaxLengthAttribute)attribute, stringLocalizer);
            }
            else if (typeof(RequiredAttribute).IsAssignableFrom(type))
            {
                adapter = new RequiredAttributeAdapter((RequiredAttribute)attribute, stringLocalizer);
            }
            else if (typeof(CompareAttribute).IsAssignableFrom(type))
            {
                adapter = new CompareAttributeAdapter((CompareAttribute)attribute, stringLocalizer);
            }
            else if (typeof(MinLengthAttribute).IsAssignableFrom(type))
            {
                adapter = new MinLengthAttributeAdapter((MinLengthAttribute)attribute, stringLocalizer);
            }
            else if (typeof(CreditCardAttribute).IsAssignableFrom(type))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-creditcard", stringLocalizer);
            }
            else if (typeof(StringLengthAttribute).IsAssignableFrom(type))
            {
                adapter = new StringLengthAttributeAdapter((StringLengthAttribute)attribute, stringLocalizer);
            }
            else if (typeof(RangeAttribute).IsAssignableFrom(type))
            {
                adapter = new RangeAttributeAdapter((RangeAttribute)attribute, stringLocalizer);
            }
            else if (typeof(EmailAddressAttribute).IsAssignableFrom(type))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-email", stringLocalizer);
            }
            else if (typeof(PhoneAttribute).IsAssignableFrom(type))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-phone", stringLocalizer);
            }
            else if (typeof(UrlAttribute).IsAssignableFrom(type))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-url", stringLocalizer);
            }
            else if (typeof(FileExtensionsAttribute).IsAssignableFrom(type))
            {
                adapter = new FileExtensionsAttributeAdapter((FileExtensionsAttribute)attribute, stringLocalizer);
            }
            else
            {
                adapter = null;
            }

            return adapter;
        }
    };
}
