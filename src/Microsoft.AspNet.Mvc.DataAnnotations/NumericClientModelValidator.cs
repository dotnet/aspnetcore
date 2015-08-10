// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An implementation of <see cref="IClientModelValidator"/> that provides the rule for validating
    /// numeric types.
    /// </summary>
    public class NumericClientModelValidator : IClientModelValidator
    {
        /// <inheritdoc />
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ClientModelValidationContext context)
        {
            return new[] { new ModelClientValidationNumericRule(GetErrorMessage(context.ModelMetadata)) };
        }

        private string GetErrorMessage([NotNull] ModelMetadata modelMetadata)
        {
            return Resources.FormatNumericClientModelValidator_FieldMustBeNumber(modelMetadata.GetDisplayName());
        }
    }
}
