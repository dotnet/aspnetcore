// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryModelBinder<TKey, TValue> : CollectionModelBinder<KeyValuePair<TKey, TValue>>
    {
        protected override bool CreateOrReplaceCollection(ModelBindingContext bindingContext,
                                                          IList<KeyValuePair<TKey, TValue>> newCollection)
        {
            CreateOrReplaceDictionary(bindingContext, newCollection, () => new Dictionary<TKey, TValue>());
            return true;
        }

        private static void CreateOrReplaceDictionary(ModelBindingContext bindingContext,
                                                      IEnumerable<KeyValuePair<TKey, TValue>> incomingElements,
                                                      Func<IDictionary<TKey, TValue>> creator)
        {
            var dictionary = bindingContext.Model as IDictionary<TKey, TValue>;
            if (dictionary == null || dictionary.IsReadOnly)
            {
                dictionary = creator();
                bindingContext.Model = dictionary;
            }

            dictionary.Clear();
            foreach (var element in incomingElements)
            {
                if (element.Key != null)
                {
                    dictionary[element.Key] = element.Value;
                }
            }
        }
    }
}
