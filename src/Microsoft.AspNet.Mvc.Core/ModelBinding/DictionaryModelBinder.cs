// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding dictionary values.
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    public class DictionaryModelBinder<TKey, TValue> : CollectionModelBinder<KeyValuePair<TKey, TValue>>
    {
        /// <inheritdoc />
        public override async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            var result = await base.BindModelAsync(bindingContext);
            if (!result.IsModelSet)
            {
                // No match for the prefix at all.
                return result;
            }

            Debug.Assert(result.Model != null);
            var model = (IDictionary<TKey, TValue>)result.Model;
            if (model.Count != 0)
            {
                // ICollection<KeyValuePair<TKey, TValue>> approach was successful.
                return result;
            }

            var enumerableValueProvider = bindingContext.ValueProvider as IEnumerableValueProvider;
            if (enumerableValueProvider == null)
            {
                // No IEnumerableValueProvider available for the fallback approach. For example the user may have
                // replaced the ValueProvider with something other than a CompositeValueProvider.
                return result;
            }

            // Attempt to bind dictionary from a set of prefix[key]=value entries. Get the short and long keys first.
            var keys = enumerableValueProvider.GetKeysFromPrefix(bindingContext.ModelName);
            if (!keys.Any())
            {
                // No entries with the expected keys.
                return result;
            }

            // Update the existing successful but empty ModelBindingResult.
            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var valueMetadata = metadataProvider.GetMetadataForType(typeof(TValue));
            var valueBindingContext = ModelBindingContext.CreateChildBindingContext(
                bindingContext,
                valueMetadata,
                fieldName: bindingContext.FieldName,
                modelName: bindingContext.ModelName,
                model: null);

            var modelBinder = bindingContext.OperationBindingContext.ModelBinder;

            var keyMappings = new Dictionary<string, TKey>(StringComparer.Ordinal);
            foreach (var kvp in keys)
            {
                // Use InvariantCulture to convert the key since ExpressionHelper.GetExpressionText() would use
                // that culture when rendering a form.
                var convertedKey = ModelBindingHelper.ConvertTo<TKey>(kvp.Key, culture: null);

                valueBindingContext.ModelName = kvp.Value;

                var valueResult = await modelBinder.BindModelAsync(valueBindingContext);

                // Always add an entry to the dictionary but validate only if binding was successful.
                model[convertedKey] = ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model);
                keyMappings.Add(kvp.Key, convertedKey);
            }

            bindingContext.ValidationState.Add(model, new ValidationStateEntry()
            {
                Strategy = new ShortFormDictionaryValidationStrategy<TKey, TValue>(keyMappings, valueMetadata),
            });

            return result;
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

            var newCollection = CreateInstance(targetType);
            CopyToModel(newCollection, collection);

            return newCollection;
        }

        /// <inheritdoc />
        protected override object CreateEmptyCollection(Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
            {
                // Simple case such as IDictionary<TKey, TValue>.
                return new Dictionary<TKey, TValue>();
            }

            return CreateInstance(targetType);
        }
    }
}
