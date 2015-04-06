// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class StringLengthAttributeAdapter : DataAnnotationsClientModelValidator<StringLengthAttribute>
    {
        public StringLengthAttributeAdapter(StringLengthAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = GetErrorMessage(context.ModelMetadata);
            var rule = new ModelClientValidationStringLengthRule(errorMessage,
                                                                 Attribute.MinimumLength,
                                                                 Attribute.MaximumLength);
            return new[] { rule };
        }
    }
}