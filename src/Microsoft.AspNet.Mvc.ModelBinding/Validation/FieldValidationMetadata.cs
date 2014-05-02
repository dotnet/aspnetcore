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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FieldValidationMetadata
    {
        private readonly List<ModelClientValidationRule> _validationRules =
            new List<ModelClientValidationRule>();
        private string _fieldName = string.Empty;

        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value ?? string.Empty; }
        }

        public bool ReplaceValidationMessageContents { get; set; }

        public string ValidationMessageId { get; set; }

        public IList<ModelClientValidationRule> ValidationRules
        {
            get { return _validationRules; }
        }
    }
}
