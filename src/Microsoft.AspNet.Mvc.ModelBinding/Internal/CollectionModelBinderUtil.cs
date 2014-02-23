using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class CollectionModelBinderUtil
    {
        public static IEnumerable<string> GetIndexNamesFromValueProviderResult(ValueProviderResult valueProviderResultIndex)
        {
            IEnumerable<string> indexNames = null;
            if (valueProviderResultIndex != null)
            {
                string[] indexes = (string[])valueProviderResultIndex.ConvertTo(typeof(string[]));
                if (indexes != null && indexes.Length > 0)
                {
                    indexNames = indexes;
                }
            }
            return indexNames;
        }

        public static void CreateOrReplaceCollection<TElement>(ModelBindingContext bindingContext,
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
            foreach (TElement element in incomingElements)
            {
                collection.Add(element);
            }
        }
    }
}
