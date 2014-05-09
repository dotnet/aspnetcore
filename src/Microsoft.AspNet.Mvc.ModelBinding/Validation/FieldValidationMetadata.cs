// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
