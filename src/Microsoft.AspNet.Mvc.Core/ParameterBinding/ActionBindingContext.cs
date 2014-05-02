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
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ActionBindingContext
    {
        public ActionBindingContext(ActionContext context,
                                    IModelMetadataProvider metadataProvider,
                                    IModelBinder modelBinder,
                                    IValueProvider valueProvider,
                                    IInputFormatterProvider inputFormatterProvider,
                                    IEnumerable<IModelValidatorProvider> validatorProviders)
        {
            ActionContext = context;
            MetadataProvider = metadataProvider;
            ModelBinder = modelBinder;
            ValueProvider = valueProvider;
            InputFormatterProvider = inputFormatterProvider;
            ValidatorProviders = validatorProviders;
        }

        public ActionContext ActionContext { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }

        public IModelBinder ModelBinder { get; private set; }

        public IValueProvider ValueProvider { get; private set; }

        public IInputFormatterProvider InputFormatterProvider { get; private set; }

        public IEnumerable<IModelValidatorProvider> ValidatorProviders { get; private set; }
    }
}
