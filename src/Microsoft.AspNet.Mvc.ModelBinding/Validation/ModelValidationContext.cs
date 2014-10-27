// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationContext
    {
        public ModelValidationContext([NotNull] ModelBindingContext bindingContext,
                                      [NotNull] ModelMetadata metadata)
            : this(bindingContext.OperationBindingContext.MetadataProvider,
                   bindingContext.OperationBindingContext.ValidatorProvider,
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
            : this(metadataProvider,
                  validatorProvider,
                  modelState,
                  metadata,
                  containerMetadata,
                  excludeFromValidationFilters: null)
        {
        }

        public ModelValidationContext([NotNull] IModelMetadataProvider metadataProvider,
                                      [NotNull] IModelValidatorProvider validatorProvider,
                                      [NotNull] ModelStateDictionary modelState,
                                      [NotNull] ModelMetadata metadata,
                                      ModelMetadata containerMetadata,
                                      IReadOnlyList<IExcludeTypeValidationFilter> excludeFromValidationFilters)
        {
            ModelMetadata = metadata;
            ModelState = modelState;
            MetadataProvider = metadataProvider;
            ValidatorProvider = validatorProvider;
            ContainerMetadata = containerMetadata;
            ExcludeFromValidationFilters = excludeFromValidationFilters;
        }

        public ModelValidationContext([NotNull] ModelValidationContext parentContext,
                                      [NotNull] ModelMetadata metadata)
        {
            ModelMetadata = metadata;
            ContainerMetadata = parentContext.ModelMetadata;
            ModelState = parentContext.ModelState;
            MetadataProvider = parentContext.MetadataProvider;
            ValidatorProvider = parentContext.ValidatorProvider;
            ExcludeFromValidationFilters = parentContext.ExcludeFromValidationFilters;
        }

        public ModelMetadata ModelMetadata { get; }

        public ModelMetadata ContainerMetadata { get; }

        public ModelStateDictionary ModelState { get; }

        public IModelMetadataProvider MetadataProvider { get; }

        public IModelValidatorProvider ValidatorProvider { get; }

        public IReadOnlyList<IExcludeTypeValidationFilter> ExcludeFromValidationFilters { get; }
    }
}
