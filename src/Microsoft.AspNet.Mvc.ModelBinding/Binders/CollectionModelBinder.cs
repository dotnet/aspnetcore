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
        public virtual async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!await bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                return null;
            }

            var valueProviderResult = await bindingContext.ValueProvider.GetValueAsync(bindingContext.ModelName);
            var bindCollectionTask = valueProviderResult != null ?
                    BindSimpleCollection(bindingContext, valueProviderResult.RawValue, valueProviderResult.Culture) :
                    BindComplexCollection(bindingContext);
            var boundCollection = await bindCollectionTask;
            var model = GetModel(boundCollection);
            return new ModelBindingResult(model, bindingContext.ModelName, true);
        }

        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        internal async Task<IEnumerable<TElement>> BindSimpleCollection(ModelBindingContext bindingContext,
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
                var innerBindingContext = new ModelBindingContext(bindingContext,
                                                                  bindingContext.ModelName,
                                                                  elementMetadata)
                {
                    ValueProvider = new CompositeValueProvider
                    {
                        // our temporary provider goes at the front of the list
                        new ElementalValueProvider(bindingContext.ModelName, rawValueElement, culture),
                        bindingContext.ValueProvider
                    }
                };

                object boundValue = null;
                var result = await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(innerBindingContext);
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

        internal async Task<IEnumerable<TElement>> BindComplexCollectionFromIndexes(ModelBindingContext bindingContext,
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
                var childBindingContext = new ModelBindingContext(bindingContext, fullChildName, elementMetadata);

                var didBind = false;
                object boundValue = null;

                var modelType = bindingContext.ModelType;

                var result = await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(childBindingContext);
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

        // Extensibility point that allows the bound collection to be manipulated or transformed before
        // being returned from the binder.
        protected virtual object GetModel(IEnumerable<TElement> newCollection)
        {
            return newCollection;
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
