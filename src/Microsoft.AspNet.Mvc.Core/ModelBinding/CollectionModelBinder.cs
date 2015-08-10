// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding collection values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the collection.</typeparam>
    public class CollectionModelBinder<TElement> : ICollectionModelBinder
    {
        /// <inheritdoc />
        public virtual async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            var model = bindingContext.Model;
            if (!await bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                // If this is the fallback case and we failed to find data for a top-level model, then generate a
                // default 'empty' model (or use existing Model) and return it.
                if (!bindingContext.IsFirstChanceBinding && bindingContext.IsTopLevelObject)
                {
                    if (model == null)
                    {
                        model = CreateEmptyCollection(bindingContext.ModelType);
                    }

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

            CollectionResult result;
            if (valueProviderResult == null)
            {
                result = await BindComplexCollection(bindingContext);
            }
            else
            {
                result = await BindSimpleCollection(
                    bindingContext,
                    valueProviderResult.RawValue,
                    valueProviderResult.Culture);
            }

            var boundCollection = result.Model;
            if (model == null)
            {
                model = ConvertToCollectionType(bindingContext.ModelType, boundCollection);
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

        /// <inheritdoc />
        public virtual bool CanCreateInstance(Type targetType)
        {
            return CreateEmptyCollection(targetType) != null;
        }

        /// <summary>
        /// Create an <see cref="object"/> assignable to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <returns>An <see cref="object"/> assignable to <paramref name="targetType"/>.</returns>
        /// <remarks>Called when creating a default 'empty' model for a top level bind.</remarks>
        protected virtual object CreateEmptyCollection(Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(List<TElement>)))
            {
                // Simple case such as ICollection<TElement>, IEnumerable<TElement> and IList<TElement>.
                return new List<TElement>();
            }

            return CreateInstance(targetType);
        }

        /// <summary>
        /// Create an instance of <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <returns>An instance of <paramref name="targetType"/>.</returns>
        protected object CreateInstance(Type targetType)
        {
            try
            {
                return Activator.CreateInstance(targetType);
            }
            catch (Exception)
            {
                // Details of exception are not important.
                return null;
            }
        }

        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        // Internal for testing.
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

        // Internal for testing.
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
                indexNames = Enumerable.Range(0, int.MaxValue)
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

        // Internal for testing.
        internal class CollectionResult
        {
            public ModelValidationNode ValidationNode { get; set; }

            public IEnumerable<TElement> Model { get; set; }
        }

        /// <summary>
        /// Gets an <see cref="object"/> assignable to <paramref name="targetType"/> that contains members from
        /// <paramref name="collection"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <param name="collection">
        /// Collection of values retrieved from value providers. Or <c>null</c> if nothing was bound.
        /// </param>
        /// <returns>
        /// An <see cref="object"/> assignable to <paramref name="targetType"/>. Or <c>null</c> if nothing was bound.
        /// </returns>
        /// <remarks>
        /// Extensibility point that allows the bound collection to be manipulated or transformed before being
        /// returned from the binder.
        /// </remarks>
        protected virtual object ConvertToCollectionType(Type targetType, IEnumerable<TElement> collection)
        {
            if (collection == null)
            {
                return null;
            }

            if (targetType.IsAssignableFrom(typeof(List<TElement>)))
            {
                // Depends on fact BindSimpleCollection() and BindComplexCollection() always return a List<TElement>
                // instance or null.
                return collection;
            }

            var newCollection = CreateInstance(targetType);
            CopyToModel(newCollection, collection);

            return newCollection;
        }

        /// <summary>
        /// Adds values from <paramref name="sourceCollection"/> to given <paramref name="target"/>.
        /// </summary>
        /// <param name="target"><see cref="object"/> into which values are copied.</param>
        /// <param name="sourceCollection">
        /// Collection of values retrieved from value providers. Or <c>null</c> if nothing was bound.
        /// </param>
        protected virtual void CopyToModel([NotNull] object target, IEnumerable<TElement> sourceCollection)
        {
            var targetCollection = target as ICollection<TElement>;
            Debug.Assert(targetCollection != null, "This binder is instantiated only for ICollection<T> model types.");

            if (sourceCollection != null && targetCollection != null && !targetCollection.IsReadOnly)
            {
                targetCollection.Clear();
                foreach (var element in sourceCollection)
                {
                    targetCollection.Add(element);
                }
            }
        }

        private static object[] RawValueToObjectArray(object rawValue)
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
