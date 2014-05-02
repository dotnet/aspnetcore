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
    public class ModelClientValidationRule
    {
        private readonly Dictionary<string, object> _validationParameters =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public ModelClientValidationRule([NotNull] string errorMessage)
            : this(validationType: string.Empty, errorMessage: errorMessage)
        {
        }

        public ModelClientValidationRule([NotNull] string validationType,
                                         [NotNull] string errorMessage)
        {
            ValidationType = validationType;
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Identifier of the <see cref="ModelClientValidationRule"/>. If client-side unobtrustive validation is
        /// enabled, use this <see langref="string"/> as part of the generated "data-val" attribute name. Must be
        /// unique in the set of enabled validation rules.
        /// </summary>
        public string ValidationType { get; private set; }

        public IDictionary<string, object> ValidationParameters
        {
            get { return _validationParameters; }
        }
    }
}
