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
            IDictionary<TKey, TValue> dictionary = bindingContext.Model as IDictionary<TKey, TValue>;
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
