// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind models of type <see cref="HttpRequestMessage"/>.
    /// </summary>
    public class HttpRequestMessageModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(HttpRequestMessage))
            {
                var model = bindingContext.OperationBindingContext.HttpContext.GetHttpRequestMessage();
                bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model);
            }

            return ModelBindingResult.NoResultAsync;
        }
    }
}
