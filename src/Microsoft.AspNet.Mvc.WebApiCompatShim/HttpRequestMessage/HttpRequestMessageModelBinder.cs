using Microsoft.AspNet.Mvc.ModelBinding;
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpRequestMessageModelBinder : IModelBinder
    {
        public Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(HttpRequestMessage))
            {
                bindingContext.Model = bindingContext.HttpContext.GetHttpRequestMessage();
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
