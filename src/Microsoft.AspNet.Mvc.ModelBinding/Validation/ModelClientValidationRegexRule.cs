// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationRegexRule : ModelClientValidationRule
    {
        private const string RegexValidationType = "regex";
        private const string RegexValidationRuleName = "pattern";

        public ModelClientValidationRegexRule(string errorMessage, string pattern)
            : base(RegexValidationType, errorMessage)
        {
            ValidationParameters.Add(RegexValidationRuleName, pattern);
        }
    }
}
