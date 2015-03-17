// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class DataAnnotationsModelValidator<TAttribute> : DataAnnotationsModelValidator
        where TAttribute : ValidationAttribute
    {
        public DataAnnotationsModelValidator(TAttribute attribute)
            : base(attribute)
        {
        }

        protected new TAttribute Attribute
        {
            get { return (TAttribute)base.Attribute; }
        }
    }
}
