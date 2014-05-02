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

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationContext
    {
        public ModelValidationContext([NotNull] ModelBindingContext bindingContext,
                                      [NotNull] ModelMetadata metadata)
            : this(bindingContext.MetadataProvider, 
                   bindingContext.ValidatorProviders, 
                   bindingContext.ModelState, 
                   metadata, 
                   bindingContext.ModelMetadata)
        {
        }

        public ModelValidationContext([NotNull] IModelMetadataProvider metadataProvider, 
                                      [NotNull] IEnumerable<IModelValidatorProvider> validatorProviders, 
                                      [NotNull] ModelStateDictionary modelState, 
                                      [NotNull] ModelMetadata metadata, 
                                      ModelMetadata containerMetadata)
        {
            ModelMetadata = metadata;
            ModelState = modelState;
            MetadataProvider = metadataProvider;
            ValidatorProviders = validatorProviders;
            ContainerMetadata = containerMetadata;
        }

        public ModelValidationContext([NotNull] ModelValidationContext parentContext,
                                      [NotNull] ModelMetadata metadata)
        {
            ModelMetadata = metadata;
            ContainerMetadata = parentContext.ModelMetadata;
            ModelState = parentContext.ModelState;
            MetadataProvider = parentContext.MetadataProvider;
            ValidatorProviders = parentContext.ValidatorProviders;
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public ModelMetadata ContainerMetadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }

        public IEnumerable<IModelValidatorProvider> ValidatorProviders { get; private set; }
    }
}
