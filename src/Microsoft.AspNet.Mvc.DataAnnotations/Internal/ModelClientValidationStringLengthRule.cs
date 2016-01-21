// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.DataAnnotations.Internal
{
    public class ModelClientValidationStringLengthRule : ModelClientValidationRule
    {
        private const string LengthValidationType = "length";
        private const string MinValidationParameter = "min";
        private const string MaxValidationParameter = "max";

        public ModelClientValidationStringLengthRule(string errorMessage, int minimumLength, int maximumLength)
            : base(LengthValidationType, errorMessage)
        {
            if (minimumLength != 0)
            {
                ValidationParameters[MinValidationParameter] = minimumLength;
            }

            if (maximumLength != int.MaxValue)
            {
                ValidationParameters[MaxValidationParameter] = maximumLength;
            }
        }
    }
}
