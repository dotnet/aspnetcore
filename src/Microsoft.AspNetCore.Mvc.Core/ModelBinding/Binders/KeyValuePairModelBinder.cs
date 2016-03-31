// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinder"/> for <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
    {
        private readonly IModelBinder _keyBinder;
        private readonly IModelBinder _valueBinder;

        /// <summary>
        /// Creates a new <see cref="KeyValuePair{TKey, TValue}"/>.
        /// </summary>
        /// <param name="keyBinder">The <see cref="IModelBinder"/> for <typeparamref name="TKey"/>.</param>
        /// <param name="valueBinder">The <see cref="IModelBinder"/> for <typeparamref name="TValue"/>.</param>
        public KeyValuePairModelBinder(IModelBinder keyBinder, IModelBinder valueBinder)
        {
            if (keyBinder == null)
            {
                throw new ArgumentNullException(nameof(keyBinder));
            }

            if (valueBinder == null)
            {
                throw new ArgumentNullException(nameof(valueBinder));
            }

            _keyBinder = keyBinder;
            _valueBinder = valueBinder;
        }

        /// <inheritdoc />
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var keyResult = await TryBindStrongModel<TKey>(bindingContext, _keyBinder, "Key");
            var valueResult = await TryBindStrongModel<TValue>(bindingContext, _valueBinder, "Value");

            if (keyResult.IsModelSet && valueResult.IsModelSet)
            {
                var model = new KeyValuePair<TKey, TValue>(
                    ModelBindingHelper.CastOrDefault<TKey>(keyResult.Model),
                    ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model));

                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
                return;
            }

            if (!keyResult.IsModelSet && valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    keyResult.Key,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());

                // Were able to get some data for this model.
                // Always tell the model binding system to skip other model binders.
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return;
            }

            if (keyResult.IsModelSet && !valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    valueResult.Key,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());

                // Were able to get some data for this model.
                // Always tell the model binding system to skip other model binders.
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
                return;
            }

            // If we failed to find data for a top-level model, then generate a
            // default 'empty' model and return it.
            if (bindingContext.IsTopLevelObject)
            {
                var model = new KeyValuePair<TKey, TValue>();
                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
            }
        }

        internal async Task<ModelBindingResult> TryBindStrongModel<TModel>(
            ModelBindingContext bindingContext,
            IModelBinder binder,
            string propertyName)
        {
            var propertyModelMetadata = bindingContext.ModelMetadata.Properties[propertyName];
            var propertyModelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, propertyName);

            using (bindingContext.EnterNestedScope(
                modelMetadata: propertyModelMetadata,
                fieldName: propertyName,
                modelName: propertyModelName,
                model: null))
            {
                await binder.BindModelAsync(bindingContext);

                var result = bindingContext.Result;
                if (result != null && result.Value.IsModelSet)
                {
                    return result.Value;
                }
                else
                {
                    return ModelBindingResult.Failed(propertyModelName);
                }
            }
        }
    }
}
