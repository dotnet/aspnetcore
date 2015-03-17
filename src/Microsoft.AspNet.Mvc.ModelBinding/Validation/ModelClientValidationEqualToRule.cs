// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Represents client side validation rule that determines if two values are equal.
    /// </summary>
    public class ModelClientValidationEqualToRule : ModelClientValidationRule
    {
        private const string EqualToValidationType = "equalto";
        private const string EqualToValidationParameter = "other";

        public ModelClientValidationEqualToRule([NotNull] string errorMessage,
                                                [NotNull] object other)
            : base(EqualToValidationType, errorMessage)
        {
            ValidationParameters[EqualToValidationParameter] = other;
        }
    }
}
