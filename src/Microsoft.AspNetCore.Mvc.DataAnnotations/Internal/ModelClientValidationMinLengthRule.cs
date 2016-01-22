// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
    public class ModelClientValidationMinLengthRule : ModelClientValidationRule
    {
        private const string MinLengthValidationType = "minlength";
        private const string MinLengthValidationParameter = "min";

        public ModelClientValidationMinLengthRule(string errorMessage, int minimumLength)
            : base(MinLengthValidationType, errorMessage)
        {
            ValidationParameters[MinLengthValidationParameter] = minimumLength;
        }
    }
}
