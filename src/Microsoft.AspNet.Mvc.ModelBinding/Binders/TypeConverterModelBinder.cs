// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class TypeConverterModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!TypeHelper.HasStringConverter(bindingContext.ModelType))
            {
                // this type cannot be converted
                return null;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);
            if (valueProviderResult == null)
            {
                return null; // no entry
            }

            object newModel;
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            try
            {
                newModel = valueProviderResult.ConvertTo(bindingContext.ModelType);
                ModelBindingHelper.ReplaceEmptyStringWithNull(bindingContext.ModelMetadata, ref newModel);
                return new ModelBindingResult(newModel, bindingContext.ModelName, true);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex);
            }

            return new ModelBindingResult(null, bindingContext.ModelName, false);
        }
    }
}
