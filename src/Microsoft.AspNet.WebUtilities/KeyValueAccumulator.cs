// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.WebUtilities
{
    public class KeyValueAccumulator<TKey, TValue>
    {
        private Dictionary<TKey, List<TValue>> _accumulator;
        IEqualityComparer<TKey> _comparer;

        public KeyValueAccumulator([NotNull] IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
            _accumulator = new Dictionary<TKey, List<TValue>>(comparer);
        }

        public void Append(TKey key, TValue value)
        {
            List<TValue> values;
            if (_accumulator.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else
            {
                _accumulator[key] = new List<TValue>(1) { value };
            }
        }

        public IDictionary<TKey, TValue[]> GetResults()
        {
            var results = new Dictionary<TKey, TValue[]>(_comparer);
            foreach (var kv in _accumulator)
            {
                results.Add(kv.Key, kv.Value.ToArray());
            }
            return results;
        }
    }
}