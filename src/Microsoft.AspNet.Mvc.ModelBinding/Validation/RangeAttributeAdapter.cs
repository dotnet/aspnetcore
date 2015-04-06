// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class RangeAttributeAdapter : DataAnnotationsClientModelValidator<RangeAttribute>
    {
        public RangeAttributeAdapter(RangeAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationRangeRule(errorMessage, Attribute.Minimum, Attribute.Maximum) };
        }
    }
}