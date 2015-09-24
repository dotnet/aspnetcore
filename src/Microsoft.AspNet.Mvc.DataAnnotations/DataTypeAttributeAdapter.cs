// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Microsoft.Framework.Localization;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A validation adapter that is used to map <see cref="DataTypeAttribute"/>'s to a single client side validation
    /// rule.
    /// </summary>
    public class DataTypeAttributeAdapter : DataAnnotationsClientModelValidator<DataTypeAttribute>
    {
        public DataTypeAttributeAdapter(DataTypeAttribute attribute, string ruleName, IStringLocalizer stringLocalizer)
            : base(attribute, stringLocalizer)
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(ruleName));
            }

            RuleName = ruleName;
        }

        public string RuleName { get; private set; }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationRule(RuleName, errorMessage) };
        }
    }
}
