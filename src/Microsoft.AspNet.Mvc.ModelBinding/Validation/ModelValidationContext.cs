// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelValidationContext
    {
        public ModelValidationContext(
            [NotNull] ModelBindingContext bindingContext,
            [NotNull] ModelExplorer modelExplorer)
            : this(bindingContext.ModelName,
                   bindingContext.OperationBindingContext.ValidatorProvider,
                   bindingContext.ModelState,
                   modelExplorer)
        {
        }

        public ModelValidationContext(
            string rootPrefix,
            [NotNull] IModelValidatorProvider validatorProvider,
            [NotNull] ModelStateDictionary modelState,
            [NotNull] ModelExplorer modelExplorer)
        {
            ModelState = modelState;
            RootPrefix = rootPrefix;
            ValidatorProvider = validatorProvider;
            ModelExplorer = modelExplorer;
        }

        public ModelValidationContext(
            [NotNull] ModelValidationContext parentContext,
            [NotNull] ModelExplorer modelExplorer)
        {
            ModelExplorer = modelExplorer;
            ModelState = parentContext.ModelState;
            RootPrefix = parentContext.RootPrefix;
            ValidatorProvider = parentContext.ValidatorProvider;
        }


        public ModelExplorer ModelExplorer { get; }

        public ModelStateDictionary ModelState { get; }

        public string RootPrefix { get; set; }

        public IModelValidatorProvider ValidatorProvider { get; }
    }
}
