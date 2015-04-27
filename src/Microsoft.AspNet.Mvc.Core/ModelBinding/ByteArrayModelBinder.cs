// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// ModelBinder to bind Byte Arrays.
    /// </summary>
    public class ByteArrayModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(byte[]))
            {
                return null;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);

            // case 1: there was no <input ... /> element containing this data
            if (valueProviderResult == null)
            {
                return null;
            }

            var value = valueProviderResult.AttemptedValue;

            // case 2: there was an <input ... /> element but it was left blank
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                var model = Convert.FromBase64String(value);
                return new ModelBindingResult(model, bindingContext.ModelName, isModelSet: true);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex);
            }

            // Matched the type (byte[]) only this binder supports.
            // Always tell the model binding system to skip other model binders i.e. return non-null.
            return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
        }
    }
}