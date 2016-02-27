// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public sealed class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (bindingContext.ModelType != typeof(KeyValuePair<TKey, TValue>))
            {
                // This binder does not apply.
                return;
            }

            var keyResult = await TryBindStrongModel<TKey>(bindingContext, "Key");
            var valueResult = await TryBindStrongModel<TValue>(bindingContext, "Value");

            if (keyResult.IsModelSet && valueResult.IsModelSet)
            {
                var model = new KeyValuePair<TKey, TValue>(
                    ModelBindingHelper.CastOrDefault<TKey>(keyResult.Model),
                    ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model));

                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
                return;
            }

            if (!keyResult.IsModelSet && valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    keyResult.Key,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());

                // Were able to get some data for this model.
                // Always tell the model binding system to skip other model binders.
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return;
            }

            if (keyResult.IsModelSet && !valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    valueResult.Key,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());

                // Were able to get some data for this model.
                // Always tell the model binding system to skip other model binders.
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return;
            }

            // If we failed to find data for a top-level model, then generate a
            // default 'empty' model and return it.
            if (bindingContext.IsTopLevelObject)
            {
                var model = new KeyValuePair<TKey, TValue>();
                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
            }
        }

        internal async Task<ModelBindingResult> TryBindStrongModel<TModel>(
            ModelBindingContext bindingContext,
            string propertyName)
        {
            var propertyModelMetadata =
                bindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(typeof(TModel));
            var propertyModelName =
                ModelNames.CreatePropertyModelName(bindingContext.ModelName, propertyName);

            using (bindingContext.EnterNestedScope(
                modelMetadata: propertyModelMetadata,
                fieldName: propertyName,
                modelName: propertyModelName,
                model: null))
            {

                await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(
                    bindingContext);
                var result = bindingContext.Result;
                if (result != null && result.Value.IsModelSet)
                {
                    return result.Value;
                }
                else
                {
                    return ModelBindingResult.Failed(propertyModelName);
                }
            }
        }
    }
}
