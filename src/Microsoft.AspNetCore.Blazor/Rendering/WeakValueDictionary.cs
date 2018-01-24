// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    internal class WeakValueDictionary<TKey, TValue> where TValue : class
    {
        private IDictionary<TKey, WeakReference<TValue>> _store
            = new Dictionary<TKey, WeakReference<TValue>>();
        private int _cullThreshold = 10;

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_store.TryGetValue(key, out var existingWeakRef))
            {
                if (existingWeakRef.TryGetTarget(out value))
                {
                    return true;
                }

                // Since we know it's not there, we might as well drop the entry now
                _store.Remove(key);
            }

            value = default(TValue);
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            if (_store.TryGetValue(key, out _))
            {
                throw new ArgumentException($"The given key was already present in the {nameof(WeakValueDictionary<TKey, TValue>)}. Key: {key}");
            }

            _store[key] = new WeakReference<TValue>(value);
            CullIfApplicable();
        }

        private void CullIfApplicable()
        {
            if (_store.Count > _cullThreshold)
            {
                var itemsToRemove = _store.Where(x => !x.Value.TryGetTarget(out _)).ToList();
                foreach (var itemToRemove in itemsToRemove)
                {
                    _store.Remove(itemToRemove.Key);
                }

                if (_store.Count > (_cullThreshold / 2))
                {
                    _cullThreshold *= 2;
                }
            }
        }
    }
}
