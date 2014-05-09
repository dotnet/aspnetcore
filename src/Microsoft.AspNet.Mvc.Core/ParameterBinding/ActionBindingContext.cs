// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
