// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Represents client side validation rule that determines if two values are equal.
    /// </summary>
    public class ModelClientValidationEqualToRule : ModelClientValidationRule
    {
        private const string EqualToValidationType = "equalto";
        private const string EqualToValidationParameter = "other";

        public ModelClientValidationEqualToRule(
            string errorMessage,
            object other)
            : base(EqualToValidationType, errorMessage)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ValidationParameters[EqualToValidationParameter] = other;
        }
    }
}
