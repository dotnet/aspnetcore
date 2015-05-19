// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

            object model;

            if (!await bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                // If this is the fallback case, and we failed to find data as a top-level model, then generate a
                // default 'empty' model and return it.
                var isTopLevelObject = bindingContext.ModelMetadata.ContainerType == null;
                var hasExplicitAlias = bindingContext.BinderModelName != null;

                if (isTopLevelObject && (hasExplicitAlias || bindingContext.ModelName == string.Empty))
                {
                    model = CreateEmptyCollection();

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

                return null;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);

            IEnumerable<TElement> boundCollection;
            CollectionResult result;
            if (valueProviderResult == null)
            {
                result = await BindComplexCollection(bindingContext);
                boundCollection = result.Model;
            }
            else
            {
                result = await BindSimpleCollection(
                    bindingContext,
                    valueProviderResult.RawValue,
                    valueProviderResult.Culture);
                boundCollection = result.Model;
            }

            model = bindingContext.Model;
            if (model == null)
            {
                model = GetModel(boundCollection);
            }
            else
            {
                // Special case for TryUpdateModelAsync(collection, ...) scenarios. Model is null in all other cases.
                CopyToModel(model, boundCollection);
            }

            return new ModelBindingResult(
                model,
                bindingContext.ModelName,
                isModelSet: true,
                validationNode: result?.ValidationNode);
        }

        // Called when we're creating a default 'empty' model for a top level bind.
        protected virtual object CreateEmptyCollection()
        {
            return new List<TElement>();
        }

        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        internal async Task<CollectionResult> BindSimpleCollection(
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

            var validationNode = new ModelValidationNode(
                bindingContext.ModelName,
                bindingContext.ModelMetadata,
                boundCollection);
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
                if (result != null && result.IsModelSet)
                {
                    boundValue = result.Model;
                    if (result.ValidationNode != null)
                    {
                        validationNode.ChildNodes.Add(result.ValidationNode);
                    }
                }
                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return new CollectionResult
            {
                ValidationNode = validationNode,
                Model = boundCollection
            };
        }

        // Used when the ValueProvider contains the collection to be bound as multiple elements, e.g. foo[0], foo[1].
        private async Task<CollectionResult> BindComplexCollection(ModelBindingContext bindingContext)
        {
            var indexPropertyName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, "index");
            var valueProviderResultIndex = await bindingContext.ValueProvider.GetValueAsync(indexPropertyName);
            var indexNames = GetIndexNamesFromValueProviderResult(valueProviderResultIndex);

            return await BindComplexCollectionFromIndexes(bindingContext, indexNames);
        }

        internal async Task<CollectionResult> BindComplexCollectionFromIndexes(
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
            var validationNode = new ModelValidationNode(
                bindingContext.ModelName,
                bindingContext.ModelMetadata,
                boundCollection);
            foreach (var indexName in indexNames)
            {
                var fullChildName = ModelNames.CreateIndexModelName(bindingContext.ModelName, indexName);
                var childBindingContext = ModelBindingContext.GetChildModelBindingContext(
                    bindingContext,
                    fullChildName,
                    elementMetadata);

                var didBind = false;
                object boundValue = null;

                var result =
                    await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(childBindingContext);
                if (result != null && result.IsModelSet)
                {
                    didBind = true;
                    boundValue = result.Model;
                    if (result.ValidationNode != null)
                    {
                        validationNode.ChildNodes.Add(result.ValidationNode);
                    }
                }

                // infinite size collection stops on first bind failure
                if (!didBind && !indexNamesIsFinite)
                {
                    break;
                }

                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return new CollectionResult
            {
                ValidationNode = validationNode,
                Model = boundCollection
            };
        }

        internal class CollectionResult
        {
            public ModelValidationNode ValidationNode { get; set; }

            public IEnumerable<TElement> Model { get; set; }
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

        private static IEnumerable<string> GetIndexNamesFromValueProviderResult(ValueProviderResult valueProviderResult)
        {
            IEnumerable<string> indexNames = null;
            if (valueProviderResult != null)
            {
                var indexes = (string[])valueProviderResult.ConvertTo(typeof(string[]));
                if (indexes != null && indexes.Length > 0)
                {
                    indexNames = indexes;
                }
            }

            return indexNames;
        }
    }
}
