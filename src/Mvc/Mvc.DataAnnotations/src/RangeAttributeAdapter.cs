// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    internal class RangeAttributeAdapter : AttributeAdapterBase<RangeAttribute>
    {
        private readonly string _max;
        private readonly string _min;

        public RangeAttributeAdapter(RangeAttribute attribute, IStringLocalizer stringLocalizer)
            : base(attribute, stringLocalizer)
        {
            // This will trigger the conversion of Attribute.Minimum and Attribute.Maximum.
            // This is needed, because the attribute is stateful and will convert from a string like
            // "100m" to the decimal value 100.
            //
            // Validate a randomly selected number.
            attribute.IsValid(3); 

            _max = Convert.ToString(Attribute.Maximum, CultureInfo.InvariantCulture);
            _min = Convert.ToString(Attribute.Minimum, CultureInfo.InvariantCulture);
        }

        public override void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-range", GetErrorMessage(context));
            MergeAttribute(context.Attributes, "data-val-range-max", _max);
            MergeAttribute(context.Attributes, "data-val-range-min", _min);
        }

        /// <inheritdoc />
        public override string GetErrorMessage(ModelValidationContextBase validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            return GetErrorMessage(
                validationContext.ModelMetadata,
                validationContext.ModelMetadata.GetDisplayName(),
                Attribute.Minimum,
                Attribute.Maximum);
        }
    }
}
