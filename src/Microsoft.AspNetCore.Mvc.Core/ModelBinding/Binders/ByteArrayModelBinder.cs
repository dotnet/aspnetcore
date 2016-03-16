// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// ModelBinder to bind byte Arrays.
    /// </summary>
    public class ByteArrayModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Check for missing data case 1: There was no <input ... /> element containing this data.
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return TaskCache.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            // Check for missing data case 2: There was an <input ... /> element but it was left blank.
            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return TaskCache.CompletedTask;
            }

            try
            {
                var model = Convert.FromBase64String(value);
                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
                return TaskCache.CompletedTask;
            }
            catch (Exception exception)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);
            }

            // Matched the type (byte[]) only this binder supports. As in missing data cases, always tell the model
            // binding system to skip other model binders i.e. return non-null.
            bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
            return TaskCache.CompletedTask;
        }
    }
}