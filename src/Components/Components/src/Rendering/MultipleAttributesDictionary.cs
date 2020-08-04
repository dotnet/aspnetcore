// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// A specialized alternative dictionary tuned entirely towards speeding up RenderTreeBuilder's
    /// ProcessDuplicateAttributes method.
    /// </summary>
    /// <remarks>
    /// It's faster than a normal Dictionary[string, int] because:
    ///
    ///  1. It uses a hash function optimized for the "no match" case. If we expect most lookups to
    ///     result in no match, which we do for attribute writing, then it's sufficient to consider
    ///     just a small fraction of the string when building a hash.
    ///  2. It has an API designed around the add-or-return-existing semantics needed for this
    ///     process, with a lot less abstraction and extensibility than Dictionary[string, int]
    /// 
    /// In total, this improves the ComplexTable benchmark by 6-7%.
    ///
    /// This dictionary shouldn't be used in other situations because it may perform much worse than
    /// a Dictionary[string, int] if most of the lookups/insertions match existing entries.
    /// </remarks>
    internal class MultipleAttributesDictionary
    {
        public const int InitialCapacity = 79;

        private string[] _keys = new string[InitialCapacity];
        private int[] _values = new int[InitialCapacity];
        private int _capacity = InitialCapacity;

        public void Clear()
        {
            Array.Clear(_keys, 0, _keys.Length);
            Array.Clear(_values, 0, _values.Length);
        }

        public bool TryAdd(string key, int value, out int existingValue)
        {
            if (TryFindIndex(key, out var index))
            {
                existingValue = _values[index];
                return false;
            }
            else
            {
                if (index < 0) // Indicates that storage is full
                {
                    ExpandStorage();
                    TryFindIndex(key, out index);
                    Debug.Assert(index >= 0);
                }

                _keys[index] = key;
                _values[index] = value;
                existingValue = default;
                return true;
            }
        }

        public void Replace(string key, int value)
        {
            if (TryFindIndex(key, out var index))
            {
                _values[index] = value;
            }
            else
            {
                throw new InvalidOperationException($"Key not found: '{key}'");
            }
        }

        private bool TryFindIndex(string key, out int existingIndexOrInsertionPosition)
        {
            var hashCode = GetSimpleHashCode(key);
            var startIndex = hashCode % _capacity;
            if (startIndex < 0)
            {
                startIndex += _capacity;
            }
            var candidateIndex = startIndex;

            do
            {
                var candidateKey = _keys[candidateIndex];
                if (candidateKey == null)
                {
                    existingIndexOrInsertionPosition = candidateIndex;
                    return false;
                }

                if (string.Equals(candidateKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    existingIndexOrInsertionPosition = candidateIndex;
                    return true;
                }

                if (++candidateIndex >= _capacity)
                {
                    candidateIndex = 0;
                }
            }
            while (candidateIndex != startIndex);

            // We didn't find the key, and there's no empty slot in which we could insert it.
            // Storage is full.
            existingIndexOrInsertionPosition = -1;
            return false;
        }

        private void ExpandStorage()
        {
            var oldKeys = _keys;
            var oldValues = _values;
            _capacity = _capacity * 2;
            _keys = new string[_capacity];
            _values = new int[_capacity];

            for (var i = 0; i < oldKeys.Length; i++)
            {
                var key = oldKeys[i];
                if (!(key is null))
                {
                    var value = oldValues[i];
                    var didInsert = TryAdd(key, value, out _);
                    Debug.Assert(didInsert);
                }
            }
        }

        private static int GetSimpleHashCode(string key)
        {
            var keyLength = key.Length;
            if (keyLength > 0)
            {
                // Consider just the first and middle characters.
                // This will produce a distinct result for a sufficiently large
                // proportion of attribute names.
                return unchecked(
                    char.ToLowerInvariant(key[0])
                    + 31 * char.ToLowerInvariant(key[keyLength / 2]));
            }
            else
            {
                return default;
            }
        }
    }
}
