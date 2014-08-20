// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationContext
    {
        public ModelValidationContext([NotNull] ModelBindingContext bindingContext,
                                      [NotNull] ModelMetadata metadata)
            : this(bindingContext.MetadataProvider,
                   bindingContext.ValidatorProvider,
                   bindingContext.ModelState,
                   metadata,
                   bindingContext.ModelMetadata)
        {
        }

        public ModelValidationContext([NotNull] IModelMetadataProvider metadataProvider,
                                      [NotNull] IModelValidatorProvider validatorProvider,
                                      [NotNull] ModelStateDictionary modelState,
                                      [NotNull] ModelMetadata metadata,
                                      ModelMetadata containerMetadata)
        {
            ModelMetadata = metadata;
            ModelState = modelState;
            MetadataProvider = metadataProvider;
            ValidatorProvider = validatorProvider;
            ContainerMetadata = containerMetadata;
        }

        public ModelValidationContext([NotNull] ModelValidationContext parentContext,
                                      [NotNull] ModelMetadata metadata)
        {
            ModelMetadata = metadata;
            ContainerMetadata = parentContext.ModelMetadata;
            ModelState = parentContext.ModelState;
            MetadataProvider = parentContext.MetadataProvider;
            ValidatorProvider = parentContext.ValidatorProvider;
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public ModelMetadata ContainerMetadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }

        public IModelValidatorProvider ValidatorProvider { get; private set; }
    }
}
