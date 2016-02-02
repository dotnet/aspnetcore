// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class SimpleTypeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult. If you need to make changes of this nature, profile
            // allocations afterwards and look for Task<ModelBindingResult>.

            if (bindingContext.ModelMetadata.IsComplexType)
            {
                // this type cannot be converted
                return TaskCache.CompletedTask;
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // no entry
                return TaskCache.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            try
            {
                var model = valueProviderResult.ConvertTo(bindingContext.ModelType);

                if (bindingContext.ModelType == typeof(string))
                {
                    var modelAsString = model as string;
                    if (bindingContext.ModelMetadata.ConvertEmptyStringToNull &&
                        string.IsNullOrEmpty(modelAsString))
                    {
                        model = null;
                    }
                }

                // When converting newModel a null value may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !bindingContext.ModelMetadata.IsReferenceOrNullableType)
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                            valueProviderResult.ToString()));

                    bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                    return TaskCache.CompletedTask;
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
                    return TaskCache.CompletedTask;
                }
            }
            catch (Exception exception)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);

                // Were able to find a converter for the type but conversion failed.
                // Tell the model binding system to skip other model binders.
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return TaskCache.CompletedTask;
            }
        }
    }
}
