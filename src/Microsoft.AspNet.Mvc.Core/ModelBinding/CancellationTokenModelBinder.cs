// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents a model binder which can bind models of type <see cref="CancellationToken"/>.
    /// </summary>
    public class CancellationTokenModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(CancellationToken))
            {
                var model = bindingContext.OperationBindingContext.HttpContext.RequestAborted;
                return Task.FromResult(new ModelBindingResult(model, bindingContext.ModelName, isModelSet: true));
            }

            return Task.FromResult<ModelBindingResult>(null);
        }
    }
}
