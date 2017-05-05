// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding dictionary values.
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    public class DictionaryModelBinder<TKey, TValue> : CollectionModelBinder<KeyValuePair<TKey, TValue>>
    {
        private readonly IModelBinder _valueBinder;

        /// <summary>
        /// Creates a new <see cref="DictionaryModelBinder{TKey, TValue}"/>.
        /// </summary>
        /// <param name="keyBinder">The <see cref="IModelBinder"/> for <typeparamref name="TKey"/>.</param>
        /// <param name="valueBinder">The <see cref="IModelBinder"/> for <typeparamref name="TValue"/>.</param>
        public DictionaryModelBinder(IModelBinder keyBinder, IModelBinder valueBinder)
            : base(new KeyValuePairModelBinder<TKey, TValue>(keyBinder, valueBinder))
        {
            if (valueBinder == null)
            {
                throw new ArgumentNullException(nameof(valueBinder));
            }

            _valueBinder = valueBinder;
        }

        /// <inheritdoc />
        public override async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            await base.BindModelAsync(bindingContext);
            if (!bindingContext.Result.IsModelSet)
            {
                // No match for the prefix at all.
                return;
            }

            var result = bindingContext.Result;

            Debug.Assert(result.Model != null);
            var model = (IDictionary<TKey, TValue>)result.Model;
            if (model.Count != 0)
            {
                // ICollection<KeyValuePair<TKey, TValue>> approach was successful.
                return;
            }

            var enumerableValueProvider = bindingContext.ValueProvider as IEnumerableValueProvider;
            if (enumerableValueProvider == null)
            {
                // No IEnumerableValueProvider available for the fallback approach. For example the user may have
                // replaced the ValueProvider with something other than a CompositeValueProvider.
                return;
            }

            // Attempt to bind dictionary from a set of prefix[key]=value entries. Get the short and long keys first.
            var keys = enumerableValueProvider.GetKeysFromPrefix(bindingContext.ModelName);
            if (keys.Count == 0)
            {
                // No entries with the expected keys.
                return;
            }

            // Update the existing successful but empty ModelBindingResult.
            var elementMetadata = bindingContext.ModelMetadata.ElementMetadata;
            var valueMetadata = elementMetadata.Properties[nameof(KeyValuePair<TKey, TValue>.Value)];

            var keyMappings = new Dictionary<string, TKey>(StringComparer.Ordinal);
            foreach (var kvp in keys)
            {
                // Use InvariantCulture to convert the key since ExpressionHelper.GetExpressionText() would use
                // that culture when rendering a form.
                var convertedKey = ModelBindingHelper.ConvertTo<TKey>(kvp.Key, culture: null);

                using (bindingContext.EnterNestedScope(
                    modelMetadata: valueMetadata,
                    fieldName: bindingContext.FieldName,
                    modelName: kvp.Value,
                    model: null))
                {
                    await _valueBinder.BindModelAsync(bindingContext);

                    var valueResult = bindingContext.Result;

                    // Always add an entry to the dictionary but validate only if binding was successful.
                    model[convertedKey] = ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model);
                    keyMappings.Add(kvp.Key, convertedKey);
                }
            }

            bindingContext.ValidationState.Add(model, new ValidationStateEntry()
            {
                Strategy = new ShortFormDictionaryValidationStrategy<TKey, TValue>(keyMappings, valueMetadata),
            });
        }

        /// <inheritdoc />
        protected override object ConvertToCollectionType(
            Type targetType,
            IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            if (collection == null)
            {
                return null;
            }

            if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
            {
                // Collection is a List<KeyValuePair<TKey, TValue>>, never already a Dictionary<TKey, TValue>.
                return collection.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return base.ConvertToCollectionType(targetType, collection);
        }

        /// <inheritdoc />
        protected override object CreateEmptyCollection(Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
            {
                // Simple case such as IDictionary<TKey, TValue>.
                return new Dictionary<TKey, TValue>();
            }

            return base.CreateEmptyCollection(targetType);
        }

        public override bool CanCreateInstance(Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
            {
                // Simple case such as IDictionary<TKey, TValue>.
                return true;
            }

            return base.CanCreateInstance(targetType);
        }
    }
}
