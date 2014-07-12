// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public static class ModelBindingHelper
    {
        /// <summary>
        /// Updates the specified model instance using the specified binder and value provider and 
        /// executes validation using the specified sequence of validator providers.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the value provider.</param>
        /// <param name="httpContext">The context for the current executing request.</param>
        /// <param name="modelState">The ModelStateDictionary used for maintaining state and 
        /// results of model-binding validation.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The model binder used for binding.</param>
        /// <param name="valueProvider">The value provider used for looking up values.</param>
        /// <param name="validatorProviders">The validator providers used for executing validation 
        /// on the model instance.</param>
        /// <returns>A Task with a value representing if the the update is successful.</returns>
        public static async Task<bool> TryUpdateModelAsync<TModel>(
                [NotNull] TModel model,
                [NotNull] string prefix,
                [NotNull] HttpContext httpContext,
                [NotNull] ModelStateDictionary modelState,
                [NotNull] IModelMetadataProvider metadataProvider,
                [NotNull] IModelBinder modelBinder,
                [NotNull] IValueProvider valueProvider,
                [NotNull] IEnumerable<IModelValidatorProvider> validatorProviders)
            where TModel : class
        {
            var modelMetadata = metadataProvider.GetMetadataForType(
                modelAccessor: null,
                modelType: typeof(TModel));

            var modelBindingContext = new ModelBindingContext
            {
                ModelMetadata = modelMetadata,
                ModelName = prefix,
                Model = model,
                ModelState = modelState,
                ModelBinder = modelBinder,
                ValueProvider = valueProvider,
                ValidatorProviders = validatorProviders,
                MetadataProvider = metadataProvider,
                FallbackToEmptyPrefix = true,
                HttpContext = httpContext
            };

            if (await modelBinder.BindModelAsync(modelBindingContext))
            {
                return modelState.IsValid;
            }

            return false;
        }
    }
}