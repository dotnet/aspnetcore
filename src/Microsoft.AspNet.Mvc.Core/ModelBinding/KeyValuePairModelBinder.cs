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

            var childNodes = new List<ModelValidationNode>();
            var keyResult = await TryBindStrongModel<TKey>(bindingContext, "Key", childNodes);
            var valueResult = await TryBindStrongModel<TValue>(bindingContext, "Value", childNodes);

            if (keyResult.IsModelSet && valueResult.IsModelSet)
            {
                var model = new KeyValuePair<TKey, TValue>(
                    ModelBindingHelper.CastOrDefault<TKey>(keyResult.Model),
                    ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model));

                // Update the model for the top level validation node.
                var modelValidationNode =
                    new ModelValidationNode(
                        bindingContext.ModelName,
                        bindingContext.ModelMetadata,
                        model,
                        childNodes);
                
                return ModelBindingResult.Success(bindingContext.ModelName, model, modelValidationNode);
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

                    var validationNode = new ModelValidationNode(
                        bindingContext.ModelName,
                        bindingContext.ModelMetadata,
                        model);

                    return ModelBindingResult.Success(bindingContext.ModelName, model, validationNode);
                }

                return ModelBindingResult.NoResult;
            }
        }

        internal async Task<ModelBindingResult> TryBindStrongModel<TModel>(
            ModelBindingContext parentBindingContext,
            string propertyName,
            List<ModelValidationNode> childNodes)
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

            var modelBindingResult = await propertyBindingContext.OperationBindingContext.ModelBinder.BindModelAsync(
                propertyBindingContext);
            if (modelBindingResult != ModelBindingResult.NoResult)
            {
                if (modelBindingResult.ValidationNode != null)
                {
                    childNodes.Add(modelBindingResult.ValidationNode);
                }

                return modelBindingResult;
            }

            return ModelBindingResult.Failed(propertyModelName);
        }
    }
}
