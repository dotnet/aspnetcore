// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;

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

            var keyModelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Key");
            var keyResult = await TryBindStrongModel<TKey>(bindingContext, _keyBinder, "Key", keyModelName);

            var valueModelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Value");
            var valueResult = await TryBindStrongModel<TValue>(bindingContext, _valueBinder, "Value", valueModelName);

            if (keyResult.IsModelSet && valueResult.IsModelSet)
            {
                var model = new KeyValuePair<TKey, TValue>(
                    ModelBindingHelper.CastOrDefault<TKey>(keyResult.Model),
                    ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model));

                bindingContext.Result = ModelBindingResult.Success(model);
                return;
            }

            if (!keyResult.IsModelSet && valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    keyModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());
                return;
            }

            if (keyResult.IsModelSet && !valueResult.IsModelSet)
            {
                bindingContext.ModelState.TryAddModelError(
                    valueModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());
                return;
            }

            // If we failed to find data for a top-level model, then generate a
            // default 'empty' model and return it.
            if (bindingContext.IsTopLevelObject)
            {
                var model = new KeyValuePair<TKey, TValue>();
                bindingContext.Result = ModelBindingResult.Success(model);
            }
        }

        internal async Task<ModelBindingResult> TryBindStrongModel<TModel>(
            ModelBindingContext bindingContext,
            IModelBinder binder,
            string propertyName,
            string propertyModelName)
        {
            var propertyModelMetadata = bindingContext.ModelMetadata.Properties[propertyName];

            using (bindingContext.EnterNestedScope(
                modelMetadata: propertyModelMetadata,
                fieldName: propertyName,
                modelName: propertyModelName,
                model: null))
            {
                await binder.BindModelAsync(bindingContext);

                return bindingContext.Result;
            }
        }
    }
}
