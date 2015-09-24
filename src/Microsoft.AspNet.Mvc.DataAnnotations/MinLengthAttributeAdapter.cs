// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Framework.Localization;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class MinLengthAttributeAdapter : DataAnnotationsClientModelValidator<MinLengthAttribute>
    {
        public MinLengthAttributeAdapter(MinLengthAttribute attribute, IStringLocalizer stringLocalizer)
            : base(attribute, stringLocalizer)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var message = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationMinLengthRule(message, Attribute.Length) };
        }
    }
}
