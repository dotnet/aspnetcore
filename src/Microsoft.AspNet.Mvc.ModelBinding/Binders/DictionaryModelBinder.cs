// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryModelBinder<TKey, TValue> : CollectionModelBinder<KeyValuePair<TKey, TValue>>
    {
        protected override object GetModel(IEnumerable<KeyValuePair<TKey, TValue>> newCollection)
        {
            return newCollection.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
