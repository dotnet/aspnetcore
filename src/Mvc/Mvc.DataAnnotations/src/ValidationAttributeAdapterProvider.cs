// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
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

            if (type == typeof(RegularExpressionAttribute))
            {
                adapter = new RegularExpressionAttributeAdapter((RegularExpressionAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(MaxLengthAttribute))
            {
                adapter = new MaxLengthAttributeAdapter((MaxLengthAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(RequiredAttribute))
            {
                adapter = new RequiredAttributeAdapter((RequiredAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(CompareAttribute))
            {
                adapter = new CompareAttributeAdapter((CompareAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(MinLengthAttribute))
            {
                adapter = new MinLengthAttributeAdapter((MinLengthAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(CreditCardAttribute))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-creditcard", stringLocalizer);
            }
            else if (type == typeof(StringLengthAttribute))
            {
                adapter = new StringLengthAttributeAdapter((StringLengthAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(RangeAttribute))
            {
                adapter = new RangeAttributeAdapter((RangeAttribute)attribute, stringLocalizer);
            }
            else if (type == typeof(EmailAddressAttribute))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-email", stringLocalizer);
            }
            else if (type == typeof(PhoneAttribute))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-phone", stringLocalizer);
            }
            else if (type == typeof(UrlAttribute))
            {
                adapter = new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "data-val-url", stringLocalizer);
            }
            else if (type == typeof(FileExtensionsAttribute))
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
