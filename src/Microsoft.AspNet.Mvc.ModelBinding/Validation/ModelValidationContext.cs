// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationContext
    {
        public ModelValidationContext([NotNull] ModelBindingContext bindingContext,
                                      [NotNull] ModelMetadata metadata)
            : this(bindingContext.ModelName,
                   bindingContext.OperationBindingContext.ValidatorProvider,
                   bindingContext.ModelState,
                   metadata,
                   bindingContext.ModelMetadata)
        {
        }

        public ModelValidationContext(string rootPrefix,
                                      [NotNull] IModelValidatorProvider validatorProvider,
                                      [NotNull] ModelStateDictionary modelState,
                                      [NotNull] ModelMetadata metadata,
                                      ModelMetadata containerMetadata)
        {
            ModelMetadata = metadata;
            ModelState = modelState;
            RootPrefix = rootPrefix;
            ValidatorProvider = validatorProvider;
            ContainerMetadata = containerMetadata;
        }

        public ModelValidationContext([NotNull] ModelValidationContext parentContext,
                                      [NotNull] ModelMetadata metadata)
        {
            ModelMetadata = metadata;
            ContainerMetadata = parentContext.ModelMetadata;
            ModelState = parentContext.ModelState;
            RootPrefix = parentContext.RootPrefix;
            ValidatorProvider = parentContext.ValidatorProvider;
        }

        public ModelMetadata ModelMetadata { get; }

        public ModelMetadata ContainerMetadata { get; }

        public ModelStateDictionary ModelState { get; }

        public string RootPrefix { get; set; }

        public IModelValidatorProvider ValidatorProvider { get; }
    }
}
