// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A validation adapter that is used to map <see cref="DataTypeAttribute"/>'s to a single client side validation
    /// rule.
    /// </summary>
    public class DataTypeAttributeAdapter : DataAnnotationsModelValidator
    {
        public DataTypeAttributeAdapter(DataTypeAttribute attribute,
                                        [NotNull] string ruleName)
            : base(attribute)
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "ruleName");
            }
            RuleName = ruleName;
        }

        public string RuleName { get; private set; }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationRule(RuleName, errorMessage) };
        }
    }
}
