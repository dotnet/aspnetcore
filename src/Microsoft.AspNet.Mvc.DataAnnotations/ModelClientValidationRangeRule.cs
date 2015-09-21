// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelClientValidationRangeRule : ModelClientValidationRule
    {
        private const string RangeValidationType = "range";
        private const string MinValidationParameter = "min";
        private const string MaxValidationParameter = "max";

        public ModelClientValidationRangeRule(
            string errorMessage,
            object minValue,
            object maxValue)
            : base(RangeValidationType, errorMessage)
        {
            if (minValue == null)
            {
                throw new ArgumentNullException(nameof(minValue));
            }

            if (maxValue == null)
            {
                throw new ArgumentNullException(nameof(maxValue));
            }

            ValidationParameters[MinValidationParameter] = minValue;
            ValidationParameters[MaxValidationParameter] = maxValue;
        }
    }
}
