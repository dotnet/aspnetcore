// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelClientValidationMaxLengthRule : ModelClientValidationRule
    {
        private const string MaxLengthValidationType = "maxlength";
        private const string MaxLengthValidationParameter = "max";

        public ModelClientValidationMaxLengthRule([NotNull] string errorMessage, int maximumLength)
            : base(MaxLengthValidationType, errorMessage)
        {
            ValidationParameters[MaxLengthValidationParameter] = maximumLength;
        }
    }
}
