// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class TypeMatchModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = await GetCompatibleValueProviderResult(bindingContext);
            if (valueProviderResult == null)
            {
                // conversion would have failed
                return null;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            var model = valueProviderResult.RawValue;
            ModelBindingHelper.ReplaceEmptyStringWithNull(bindingContext.ModelMetadata, ref model);
            return new ModelBindingResult(model, bindingContext.ModelName, isModelSet: true);
        }

        internal static async Task<ValueProviderResult> GetCompatibleValueProviderResult(ModelBindingContext context)
        {
            ModelBindingHelper.ValidateBindingContext(context);

            var valueProviderResult = await context.ValueProvider.GetValueAsync(context.ModelName);
            if (valueProviderResult == null)
            {
                return null; // the value doesn't exist
            }

            if (!context.ModelType.IsCompatibleWith(valueProviderResult.RawValue))
            {
                return null; // value is of incompatible type
            }

            return valueProviderResult;
        }
    }
}
