// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelClientValidationRequiredRule : ModelClientValidationRule
    {
        private const string RequiredValidationType = "required";

        public ModelClientValidationRequiredRule(string errorMessage) :
            base(RequiredValidationType, errorMessage)
        {
        }
    }
}
