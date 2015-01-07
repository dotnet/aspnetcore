// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CollectionModelBinder<TElement> : IModelBinder
    {
        public virtual async Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!await bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                return false;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);
            var bindCollectionTask = valueProviderResult != null ?
                    BindSimpleCollection(bindingContext, valueProviderResult.RawValue, valueProviderResult.Culture) :
                    BindComplexCollection(bindingContext);
            var boundCollection = await bindCollectionTask;

            return CreateOrReplaceCollection(bindingContext, boundCollection);
        }

        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        internal async Task<List<TElement>> BindSimpleCollection(ModelBindingContext bindingContext,
                                                                 object rawValue,
                                                                 CultureInfo culture)
        {
            if (rawValue == null)
            {
                return null; // nothing to do
            }

            var boundCollection = new List<TElement>();

            var rawValueArray = RawValueToObjectArray(rawValue);
            foreach (var rawValueElement in rawValueArray)
            {
                var innerModelMetadata =
                    bindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(null, typeof(TElement));
                var innerBindingContext = new ModelBindingContext(bindingContext,
                                                                  bindingContext.ModelName,
                                                                  innerModelMetadata)
                {
                    ValueProvider = new CompositeValueProvider
                    {
                        // our temporary provider goes at the front of the list
                        new ElementalValueProvider(bindingContext.ModelName, rawValueElement, culture),
                        bindingContext.ValueProvider
                    }
                };

                object boundValue = null;
                if (await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(innerBindingContext))
                {
                    boundValue = innerBindingContext.Model;
                    bindingContext.ValidationNode.ChildNodes.Add(innerBindingContext.ValidationNode);
                }
                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return boundCollection;
        }

        // Used when the ValueProvider contains the collection to be bound as multiple elements, e.g. foo[0], foo[1].
        private async Task<List<TElement>> BindComplexCollection(ModelBindingContext bindingContext)
        {
            var indexPropertyName = ModelBindingHelper.CreatePropertyModelName(bindingContext.ModelName, "index");
            var valueProviderResultIndex = await bindingContext.ValueProvider.GetValueAsync(indexPropertyName);
            var indexNames = CollectionModelBinderUtil.GetIndexNamesFromValueProviderResult(valueProviderResultIndex);
            return await BindComplexCollectionFromIndexes(bindingContext, indexNames);
        }

        internal async Task<List<TElement>> BindComplexCollectionFromIndexes(ModelBindingContext bindingContext,
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

            var boundCollection = new List<TElement>();
            foreach (var indexName in indexNames)
            {
                var fullChildName = ModelBindingHelper.CreateIndexModelName(bindingContext.ModelName, indexName);
                var childModelMetadata =
                    bindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(null, typeof(TElement));
                var childBindingContext = new ModelBindingContext(bindingContext, fullChildName, childModelMetadata);

                var didBind = false;
                object boundValue = null;

                var modelType = bindingContext.ModelType;

                if (await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(childBindingContext))
                {
                    didBind = true;
                    boundValue = childBindingContext.Model;

                    // merge validation up
                    bindingContext.ValidationNode.ChildNodes.Add(childBindingContext.ValidationNode);
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

        // Extensibility point that allows the bound collection to be manipulated or transformed before
        // being returned from the binder.
        protected virtual bool CreateOrReplaceCollection(ModelBindingContext bindingContext,
                                                         IList<TElement> newCollection)
        {
            CreateOrReplaceCollection(bindingContext, newCollection, () => new List<TElement>());
            return true;
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

        internal static void CreateOrReplaceCollection(ModelBindingContext bindingContext,
                                                                 IEnumerable<TElement> incomingElements,
                                                                 Func<ICollection<TElement>> creator)
        {
            var collection = bindingContext.Model as ICollection<TElement>;
            if (collection == null || collection.IsReadOnly)
            {
                collection = creator();
                bindingContext.Model = collection;
            }

            collection.Clear();
            foreach (var element in incomingElements)
            {
                collection.Add(element);
            }
        }
    }
}
