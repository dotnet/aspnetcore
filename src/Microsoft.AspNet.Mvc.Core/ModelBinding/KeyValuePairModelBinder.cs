// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext,
                                                      typeof(KeyValuePair<TKey, TValue>),
                                                      allowNullModel: true);

            var keyResult = await TryBindStrongModel<TKey>(bindingContext, "Key");
            var valueResult = await TryBindStrongModel<TValue>(bindingContext, "Value");

            if (keyResult.IsModelSet && valueResult.IsModelSet)
            {
                var model = new KeyValuePair<TKey, TValue>(
                    ModelBindingHelper.CastOrDefault<TKey>(keyResult.Model),
                    ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model));

                return ModelBindingResult.Success(bindingContext.ModelName, model);
            }
            else if (!keyResult.IsModelSet && valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    keyResult.Key,
                    Resources.KeyValuePair_BothKeyAndValueMustBePresent);

                // Were able to get some data for this model.
                // Always tell the model binding system to skip other model binders.
                return ModelBindingResult.Failed(bindingContext.ModelName);
            }
            else if (keyResult.IsModelSet && !valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    valueResult.Key,
                    Resources.KeyValuePair_BothKeyAndValueMustBePresent);

                // Were able to get some data for this model.
                // Always tell the model binding system to skip other model binders.
                return ModelBindingResult.Failed(bindingContext.ModelName);
            }
            else
            {
                // If we failed to find data for a top-level model, then generate a
                // default 'empty' model and return it.
                if (bindingContext.IsTopLevelObject)
                {
                    var model = new KeyValuePair<TKey, TValue>();
                    return ModelBindingResult.Success(bindingContext.ModelName, model);
                }

                return ModelBindingResult.NoResult;
            }
        }

        internal async Task<ModelBindingResult> TryBindStrongModel<TModel>(
            ModelBindingContext parentBindingContext,
            string propertyName)
        {
            var propertyModelMetadata =
                parentBindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(typeof(TModel));
            var propertyModelName =
                ModelNames.CreatePropertyModelName(parentBindingContext.ModelName, propertyName);
            var propertyBindingContext = ModelBindingContext.CreateChildBindingContext(
                parentBindingContext,
                propertyModelMetadata,
                propertyName,
                propertyModelName,
                model: null);

            var result = await propertyBindingContext.OperationBindingContext.ModelBinder.BindModelAsync(
                propertyBindingContext);
            if (result.IsModelSet)
            {
                return result;
            }
            else
            {
                return ModelBindingResult.Failed(propertyModelName);
            }
        }
    }
}
