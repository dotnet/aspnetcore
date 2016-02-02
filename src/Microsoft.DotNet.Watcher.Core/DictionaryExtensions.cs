// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
        {
            lock (dictionary)
            {
                TValue value;
                if (!dictionary.TryGetValue(key, out value))
                {
                    value = factory(key);
                    dictionary[key] = value;
                }

                return value;
            }
        }
    }
}
