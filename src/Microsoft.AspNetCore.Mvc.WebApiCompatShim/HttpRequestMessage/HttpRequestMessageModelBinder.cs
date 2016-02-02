// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind models of type <see cref="HttpRequestMessage"/>.
    /// </summary>
    public class HttpRequestMessageModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(HttpRequestMessage))
            {
                var model = bindingContext.OperationBindingContext.HttpContext.GetHttpRequestMessage();
                bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
            }

            return TaskCache.CompletedTask;
        }
    }
}
