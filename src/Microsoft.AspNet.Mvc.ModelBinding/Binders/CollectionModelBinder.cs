// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding collection values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the collection.</typeparam>
    public class CollectionModelBinder<TElement> : IModelBinder
    {
        /// <inheritdoc />
        public virtual async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!await bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                return null;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);

            IEnumerable<TElement> boundCollection;
            if (valueProviderResult == null)
            {
                boundCollection = await BindComplexCollection(bindingContext);
            }
            else
            {
                boundCollection = await BindSimpleCollection(
                    bindingContext,
                    valueProviderResult.RawValue,
                    valueProviderResult.Culture);
            }

            var model = bindingContext.Model;
            if (model == null)
            {
                model = GetModel(boundCollection);
            }
            else
            {
                // Special case for TryUpdateModelAsync(collection, ...) scenarios. Model is null in all other cases.
                CopyToModel(model, boundCollection);
            }

            return new ModelBindingResult(model, bindingContext.ModelName, isModelSet: true);
        }

        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        internal async Task<IEnumerable<TElement>> BindSimpleCollection(
            ModelBindingContext bindingContext,
            object rawValue,
            CultureInfo culture)
        {
            if (rawValue == null)
            {
                return null; // nothing to do
            }

            var boundCollection = new List<TElement>();

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var elementMetadata = metadataProvider.GetMetadataForType(typeof(TElement));

            var rawValueArray = RawValueToObjectArray(rawValue);
            foreach (var rawValueElement in rawValueArray)
            {
                var innerBindingContext = ModelBindingContext.GetChildModelBindingContext(
                    bindingContext,
                    bindingContext.ModelName,
                    elementMetadata);
                innerBindingContext.ValueProvider = new CompositeValueProvider
                {
                    // our temporary provider goes at the front of the list
                    new ElementalValueProvider(bindingContext.ModelName, rawValueElement, culture),
                    bindingContext.ValueProvider
                };

                object boundValue = null;
                var result =
                    await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(innerBindingContext);
                if (result != null)
                {
                    boundValue = result.Model;
                }
                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return boundCollection;
        }

        // Used when the ValueProvider contains the collection to be bound as multiple elements, e.g. foo[0], foo[1].
        private async Task<IEnumerable<TElement>> BindComplexCollection(ModelBindingContext bindingContext)
        {
            var indexPropertyName = ModelBindingHelper.CreatePropertyModelName(bindingContext.ModelName, "index");
            var valueProviderResultIndex = await bindingContext.ValueProvider.GetValueAsync(indexPropertyName);
            var indexNames = CollectionModelBinderUtil.GetIndexNamesFromValueProviderResult(valueProviderResultIndex);

            return await BindComplexCollectionFromIndexes(bindingContext, indexNames);
        }

        internal async Task<IEnumerable<TElement>> BindComplexCollectionFromIndexes(
            ModelBindingContext bindingContext,
            IEnumerable<string> indexNames)
        {
            bool indexNamesIsFinite;
            if (indexNames != null)
            {
                indexNamesIsFinite = true;
            }
            else
            {
                indexNamesIsFinite = false;
                indexNames = Enumerable.Range(0, Int32.MaxValue)
                                       .Select(i => i.ToString(CultureInfo.InvariantCulture));
            }

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var elementMetadata = metadataProvider.GetMetadataForType(typeof(TElement));

            var boundCollection = new List<TElement>();
            foreach (var indexName in indexNames)
            {
                var fullChildName = ModelBindingHelper.CreateIndexModelName(bindingContext.ModelName, indexName);
                var childBindingContext = ModelBindingContext.GetChildModelBindingContext(
                    bindingContext,
                    fullChildName,
                    elementMetadata);

                var didBind = false;
                object boundValue = null;

                var modelType = bindingContext.ModelType;

                var result =
                    await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(childBindingContext);
                if (result != null)
                {
                    didBind = true;
                    boundValue = result.Model;
                }

                // infinite size collection stops on first bind failure
                if (!didBind && !indexNamesIsFinite)
                {
                    break;
                }

                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return boundCollection;
        }

        /// <summary>
        /// Gets an <see cref="object"/> assignable to the collection property.
        /// </summary>
        /// <param name="newCollection">
        /// Collection of values retrieved from value providers. Or <c>null</c> if nothing was bound.
        /// </param>
        /// <returns>
        /// <see cref="object"/> assignable to the collection property. Or <c>null</c> if nothing was bound.
        /// </returns>
        /// <remarks>
        /// Extensibility point that allows the bound collection to be manipulated or transformed before being
        /// returned from the binder.
        /// </remarks>
        protected virtual object GetModel(IEnumerable<TElement> newCollection)
        {
            // Depends on fact BindSimpleCollection() and BindComplexCollection() always return a List<TElement>
            // instance or null. In addition GenericModelBinder confirms a List<TElement> is assignable to the
            // property prior to instantiating this binder and subclass binders do not call this method.
            return newCollection;
        }

        /// <summary>
        /// Adds values from <paramref name="sourceCollection"/> to given <paramref name="target"/>.
        /// </summary>
        /// <param name="target"><see cref="object"/> into which values are copied.</param>
        /// <param name="sourceCollection">
        /// Collection of values retrieved from value providers. Or <c>null</c> if nothing was bound.
        /// </param>
        /// <remarks>Called only in TryUpdateModelAsync(collection, ...) scenarios.</remarks>
        protected virtual void CopyToModel([NotNull] object target, IEnumerable<TElement> sourceCollection)
        {
            var targetCollection = target as ICollection<TElement>;
            Debug.Assert(targetCollection != null); // This binder is instantiated only for ICollection model types.

            if (sourceCollection != null && targetCollection != null && !targetCollection.IsReadOnly)
            {
                targetCollection.Clear();
                foreach (var element in sourceCollection)
                {
                    targetCollection.Add(element);
                }
            }
        }

        internal static object[] RawValueToObjectArray(object rawValue)
        {
            // precondition: rawValue is not null

            // Need to special-case String so it's not caught by the IEnumerable check which follows
            if (rawValue is string)
            {
                return new[] { rawValue };
            }

            var rawValueAsObjectArray = rawValue as object[];
            if (rawValueAsObjectArray != null)
            {
                return rawValueAsObjectArray;
            }

            var rawValueAsEnumerable = rawValue as IEnumerable;
            if (rawValueAsEnumerable != null)
            {
                return rawValueAsEnumerable.Cast<object>().ToArray();
            }

            // fallback
            return new[] { rawValue };
        }
    }
}
