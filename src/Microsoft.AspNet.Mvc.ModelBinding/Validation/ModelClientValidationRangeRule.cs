// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelClientValidationRangeRule : ModelClientValidationRule
    {
        private const string RangeValidationType = "range";
        private const string MinValidationParameter = "min";
        private const string MaxValidationParameter = "max";

        public ModelClientValidationRangeRule([NotNull] string errorMessage,
                                              [NotNull] object minValue,
                                              [NotNull] object maxValue)
            : base(RangeValidationType, errorMessage)
        {
            ValidationParameters[MinValidationParameter] = minValue;
            ValidationParameters[MaxValidationParameter] = maxValue;
        }
    }
}
