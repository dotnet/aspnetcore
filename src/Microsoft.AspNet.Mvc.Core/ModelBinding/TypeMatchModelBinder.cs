// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

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
            var validationNode = new ModelValidationNode(
                  bindingContext.ModelName,
                  bindingContext.ModelMetadata,
                  model);

            return new ModelBindingResult(
                model,
                bindingContext.ModelName,
                isModelSet: true,
                validationNode: validationNode);
        }

        internal static async Task<ValueProviderResult> GetCompatibleValueProviderResult(ModelBindingContext context)
        {
            ModelBindingHelper.ValidateBindingContext(context);

            var valueProviderResult = await context.ValueProvider.GetValueAsync(context.ModelName);
            if (valueProviderResult == null)
            {
                return null; // the value doesn't exist
            }

            if (!IsCompatibleWith(context.ModelType, valueProviderResult.RawValue))
            {
                return null; // value is of incompatible type
            }

            return valueProviderResult;
        }

        private static bool IsCompatibleWith([NotNull] Type type, object value)
        {
            if (value == null)
            {
                return !type.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(type) != null;
            }
            else
            {
                return type.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo());
            }
        }
    }
}
