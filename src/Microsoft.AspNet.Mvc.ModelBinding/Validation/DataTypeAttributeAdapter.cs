// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A validation adapter that is used to map <see cref="DataTypeAttribute"/>'s to a single client side validation 
    /// rule.
    /// </summary>
    public class DataTypeAttributeAdapter : DataAnnotationsModelValidator
    {
        public DataTypeAttributeAdapter(DataTypeAttribute attribute,
                                        [NotNull] string ruleName)
            : base(attribute)
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "ruleName");
            }
            RuleName = ruleName;
        }

        public string RuleName { get; private set; }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationRule(RuleName, errorMessage) };
        }
    }
}
