// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class MaxLengthAttributeAdapter : DataAnnotationsClientModelValidator<MaxLengthAttribute>
    {
        public MaxLengthAttributeAdapter(MaxLengthAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var message = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationMaxLengthRule(message, Attribute.Length) };
        }
    }
}
