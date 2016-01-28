// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind models of type <see cref="CancellationToken"/>.
    /// </summary>
    public class CancellationTokenModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(CancellationToken))
            {
                // We need to force boxing now, so we can insert the same reference to the boxed CancellationToken
                // in both the ValidationState and ModelBindingResult.
                //
                // DO NOT simplify this code by removing the cast.
                var model = (object)bindingContext.OperationBindingContext.HttpContext.RequestAborted;
                bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model);
            }

            return ModelBindingResult.NoResultAsync;
        }
    }
}
