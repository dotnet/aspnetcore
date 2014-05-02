// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
