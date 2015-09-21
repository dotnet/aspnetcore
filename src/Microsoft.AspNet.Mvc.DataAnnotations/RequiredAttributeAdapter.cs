// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class RequiredAttributeAdapter : DataAnnotationsClientModelValidator<RequiredAttribute>
    {
        public RequiredAttributeAdapter(RequiredAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
           ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationRequiredRule(errorMessage) };
        }
    }
}