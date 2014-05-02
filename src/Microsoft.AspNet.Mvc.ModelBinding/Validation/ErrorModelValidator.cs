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

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IModelValidator"/> to represent an error. This validator will always throw an exception regardless 
    /// of the actual model value.
    /// This is used to perform meta-validation - that is to verify the validation attributes make sense.
    /// </summary>
    public class ErrorModelValidator : IModelValidator
    {
        private readonly string _errorMessage;

        public ErrorModelValidator([NotNull] string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public bool IsRequired { get { return false; } }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            throw new InvalidOperationException(_errorMessage);
        }
    }
}
