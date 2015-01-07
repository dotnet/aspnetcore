// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
    {
        public async Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext,
                                                      typeof(KeyValuePair<TKey, TValue>),
                                                      allowNullModel: true);

            var keyResult = await TryBindStrongModel<TKey>(bindingContext, "key");
            var valueResult = await TryBindStrongModel<TValue>(bindingContext, "value");

            if (keyResult.Success && valueResult.Success)
            {
                bindingContext.Model = new KeyValuePair<TKey, TValue>(keyResult.Model, valueResult.Model);
            }
            return keyResult.Success || valueResult.Success;
        }

        internal async Task<BindResult<TModel>> TryBindStrongModel<TModel>(ModelBindingContext parentBindingContext,
                                                                          string propertyName)
        {
            var propertyModelMetadata =
                parentBindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(modelAccessor: null,
                                                                                            modelType: typeof(TModel));
            var propertyModelName =
                ModelBindingHelper.CreatePropertyModelName(parentBindingContext.ModelName, propertyName);
            var propertyBindingContext =
                new ModelBindingContext(parentBindingContext, propertyModelName, propertyModelMetadata);

            if (await propertyBindingContext.OperationBindingContext.ModelBinder.BindModelAsync(propertyBindingContext))
            {
                var untypedModel = propertyBindingContext.Model;
                var model = ModelBindingHelper.CastOrDefault<TModel>(untypedModel);
                parentBindingContext.ValidationNode.ChildNodes.Add(propertyBindingContext.ValidationNode);
                return new BindResult<TModel>(success: true, model: model);
            }

            return new BindResult<TModel>(success: false, model: default(TModel));
        }

        internal sealed class BindResult<TModel>
        {
            public BindResult(bool success, TModel model)
            {
                Success = success;
                Model = model;
            }

            public bool Success { get; private set; }

            public TModel Model { get; private set; }
        }
    }
}
