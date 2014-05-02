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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
    {
        public async Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext, typeof(KeyValuePair<TKey, TValue>), allowNullModel: true);

            var keyResult = await TryBindStrongModel<TKey>(bindingContext, "key");
            var valueResult =  await TryBindStrongModel<TValue>(bindingContext, "value");

            if (keyResult.Success && valueResult.Success)
            {
                bindingContext.Model = new KeyValuePair<TKey, TValue>(keyResult.Model, valueResult.Model);
            }
            return keyResult.Success || valueResult.Success;
        }

        internal async Task<BindResult<TModel>> TryBindStrongModel<TModel>(ModelBindingContext parentBindingContext,
                                                                          string propertyName)
        {
            ModelBindingContext propertyBindingContext = new ModelBindingContext(parentBindingContext)
            {
                ModelMetadata = parentBindingContext.MetadataProvider.GetMetadataForType(modelAccessor: null, modelType: typeof(TModel)),
                ModelName = ModelBindingHelper.CreatePropertyModelName(parentBindingContext.ModelName, propertyName)
            };

            if (await propertyBindingContext.ModelBinder.BindModelAsync(propertyBindingContext))
            {
                object untypedModel = propertyBindingContext.Model;
                var model = ModelBindingHelper.CastOrDefault<TModel>(untypedModel);
                parentBindingContext.ValidationNode.ChildNodes.Add(propertyBindingContext.ValidationNode);
                return new BindResult<TModel>(true, model);
            }

            return new BindResult<TModel>(false, default(TModel));
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
