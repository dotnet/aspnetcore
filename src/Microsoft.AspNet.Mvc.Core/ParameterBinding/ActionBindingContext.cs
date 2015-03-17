// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc
{
    public class ActionBindingContext
    {
        public IModelBinder ModelBinder { get; set; }

        public IValueProvider ValueProvider { get; set; }

        public IList<IInputFormatter> InputFormatters { get; set; }

        public IModelValidatorProvider ValidatorProvider { get; set; }
    }
}
