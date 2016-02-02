// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request services when a model 
    /// has the binding source <see cref="BindingSource.Services"/>/
    /// </summary>
    public class ServicesModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult. If you need to make changes of this nature, profile
            // allocations afterwards and look for Task<ModelBindingResult>.

            var allowedBindingSource = bindingContext.BindingSource;
            if (allowedBindingSource == null ||
                !allowedBindingSource.CanAcceptDataFrom(BindingSource.Services))
            {
                // Services are opt-in. This model either didn't specify [FromService] or specified something
                // incompatible so let other binders run.
                return TaskCache.CompletedTask;
            }

            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;
            var model = requestServices.GetRequiredService(bindingContext.ModelType);

            bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });

            bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
            return TaskCache.CompletedTask;
        }
    }
}
