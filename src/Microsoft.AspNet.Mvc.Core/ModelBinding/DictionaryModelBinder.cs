// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding dictionary values.
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    public class DictionaryModelBinder<TKey, TValue> : CollectionModelBinder<KeyValuePair<TKey, TValue>>
    {
        /// <inheritdoc />
        protected override object GetModel(IEnumerable<KeyValuePair<TKey, TValue>> newCollection)
        {
            return newCollection?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        protected override object CreateEmptyCollection()
        {
            return new Dictionary<TKey, TValue>();
        }
    }
}
