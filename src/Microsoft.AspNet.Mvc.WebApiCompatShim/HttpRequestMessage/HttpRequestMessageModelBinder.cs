// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpRequestMessageModelBinder : IModelBinder
    {
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(HttpRequestMessage))
            {
                var model = bindingContext.OperationBindingContext.HttpContext.GetHttpRequestMessage();
                return Task.FromResult(new ModelBindingResult(model, bindingContext.ModelName, isModelSet: true));
            }

            return Task.FromResult<ModelBindingResult>(null);
        }
    }
}
