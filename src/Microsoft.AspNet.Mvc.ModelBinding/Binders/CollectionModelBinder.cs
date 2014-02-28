using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CollectionModelBinder<TElement> : IModelBinder
    {
        public virtual bool BindModel(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
            {
                return false;
            }

            ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            List<TElement> boundCollection = (valueProviderResult != null)
                                                 ? BindSimpleCollection(bindingContext, valueProviderResult.RawValue, valueProviderResult.Culture)
                                                 : BindComplexCollection(bindingContext);

            bool retVal = CreateOrReplaceCollection(bindingContext, boundCollection);
            return retVal;
        }

        // TODO: Make this method internal
        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        public List<TElement> BindSimpleCollection(ModelBindingContext bindingContext,
                                                   object rawValue,
                                                   CultureInfo culture)
        {
            if (rawValue == null)
            {
                return null; // nothing to do
            }

            List<TElement> boundCollection = new List<TElement>();

            object[] rawValueArray = RawValueToObjectArray(rawValue);
            foreach (object rawValueElement in rawValueArray)
            {
                ModelBindingContext innerBindingContext = new ModelBindingContext(bindingContext)
                {
                    ModelMetadata = bindingContext.MetadataProvider.GetMetadataForType(null, typeof(TElement)),
                    ModelName = bindingContext.ModelName,
                    ValueProvider = new CompositeValueProvider
                    {
                        // our temporary provider goes at the front of the list
                        new ElementalValueProvider(bindingContext.ModelName, rawValueElement, culture), 
                        bindingContext.ValueProvider
                    }
                };

                object boundValue = null;
                if (bindingContext.ModelBinder.BindModel(innerBindingContext))
                {
                    boundValue = innerBindingContext.Model;
                    bindingContext.ValidationNode.ChildNodes.Add(innerBindingContext.ValidationNode);
                }
                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return boundCollection;
        }

        // Used when the ValueProvider contains the collection to be bound as multiple elements, e.g. foo[0], foo[1].
        private List<TElement> BindComplexCollection(ModelBindingContext bindingContext)
        {
            string indexPropertyName = ModelBindingHelper.CreatePropertyModelName(bindingContext.ModelName, "index");
            ValueProviderResult valueProviderResultIndex = bindingContext.ValueProvider.GetValue(indexPropertyName);
            IEnumerable<string> indexNames = CollectionModelBinderUtil.GetIndexNamesFromValueProviderResult(valueProviderResultIndex);
            return BindComplexCollectionFromIndexes(bindingContext, indexNames);
        }

        // TODO: Convert to internal
        public List<TElement> BindComplexCollectionFromIndexes(ModelBindingContext bindingContext,
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

            List<TElement> boundCollection = new List<TElement>();
            foreach (string indexName in indexNames)
            {
                string fullChildName = ModelBindingHelper.CreateIndexModelName(bindingContext.ModelName, indexName);
                var childBindingContext = new ModelBindingContext(bindingContext)
                {
                    ModelMetadata = bindingContext.MetadataProvider.GetMetadataForType(null, typeof(TElement)),
                    ModelName = fullChildName
                };

                bool didBind = false;
                object boundValue = null;

                Type modelType = bindingContext.ModelType;

                if (bindingContext.ModelBinder.BindModel(childBindingContext))
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
        protected virtual bool CreateOrReplaceCollection(ModelBindingContext bindingContext, IList<TElement> newCollection)
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

            object[] rawValueAsObjectArray = rawValue as object[];
            if (rawValueAsObjectArray != null)
            {
                return rawValueAsObjectArray;
            }

            IEnumerable rawValueAsEnumerable = rawValue as IEnumerable;
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
            ICollection<TElement> collection = bindingContext.Model as ICollection<TElement>;
            if (collection == null || collection.IsReadOnly)
            {
                collection = creator();
                bindingContext.Model = collection;
            }

            collection.Clear();
            foreach (TElement element in incomingElements)
            {
                collection.Add(element);
            }
        }
    }
}
