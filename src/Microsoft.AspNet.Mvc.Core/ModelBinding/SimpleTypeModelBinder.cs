// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class SimpleTypeModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata.IsComplexType)
            {
                // this type cannot be converted
                return null;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // no entry
                return null;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            try
            {
                var model = valueProviderResult.ConvertTo(bindingContext.ModelType);

                if (bindingContext.ModelType == typeof(string))
                {
                    var modelAsString = model as string;
                    if (bindingContext.ModelMetadata.ConvertEmptyStringToNull &&
                        string.IsNullOrWhiteSpace(modelAsString))
                    {
                        model = null;
                    }
                }

                var isModelSet = true;

                // When converting newModel a null value may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !AllowsNullValue(bindingContext.ModelType))
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        Resources.FormatCommon_ValueNotValidForProperty(model));

                    isModelSet = false;
                }

                // Include a ModelValidationNode if binding succeeded.
                var validationNode = isModelSet ?
                    new ModelValidationNode(bindingContext.ModelName, bindingContext.ModelMetadata, model) :
                    null;

                return new ModelBindingResult(model, bindingContext.ModelName, isModelSet, validationNode);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex);
            }

            // Were able to find a converter for the type but conversion failed.
            // Tell the model binding system to skip other model binders i.e. return non-null.
            return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
        }

        private static bool AllowsNullValue(Type type)
        {
            return !type.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(type) != null;
        }
    }
}
