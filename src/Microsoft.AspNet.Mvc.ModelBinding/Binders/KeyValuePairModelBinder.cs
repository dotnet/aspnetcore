// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

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

                return new ModelBindingResult(model, bindingContext.ModelName, isModelSet: true);
            }
            else if (!keyResult.IsModelSet && valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    keyResult.Key,
                    Resources.KeyValuePair_BothKeyAndValueMustBePresent);
                return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
            }
            else if (keyResult.IsModelSet && !valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    valueResult.Key,
                    Resources.KeyValuePair_BothKeyAndValueMustBePresent);
                return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
            }
            else
            {
                return null;
            }
        }

        internal async Task<ModelBindingResult> TryBindStrongModel<TModel>(ModelBindingContext parentBindingContext,
                                                                          string propertyName)
        {
            var propertyModelMetadata =
                parentBindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(typeof(TModel));
            var propertyModelName =
                ModelBindingHelper.CreatePropertyModelName(parentBindingContext.ModelName, propertyName);
            var propertyBindingContext =
                new ModelBindingContext(parentBindingContext, propertyModelName, propertyModelMetadata);
            var modelBindingResult = 
                await propertyBindingContext.OperationBindingContext.ModelBinder.BindModelAsync(propertyBindingContext);
            if (modelBindingResult != null)
            {
                return modelBindingResult;
            }

            return new ModelBindingResult(model: default(TModel), key: propertyModelName, isModelSet: false);
        }
    }
}
