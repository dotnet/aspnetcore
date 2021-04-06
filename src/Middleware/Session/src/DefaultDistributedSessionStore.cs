// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Session
{
    internal sealed class DefaultDistributedSessionStore : IDistributedSessionStore
    {
        private readonly Dictionary<EncodedKey, byte[]> _store = new Dictionary<EncodedKey, byte[]>();

        public int Count => _store.Count;

        public ICollection<EncodedKey> Keys => _store.Keys;

        public bool TryGetValue(EncodedKey key, [MaybeNullWhen(false)] out byte[] value)
            => _store.TryGetValue(key, out value);

        public void SetValue(EncodedKey key, byte[] value) => _store[key] = value;

        public bool Remove(EncodedKey encodedKey)
            => _store.Remove(encodedKey);

        public void Clear()
            => _store.Clear();

        public IEnumerator<KeyValuePair<EncodedKey, byte[]>> GetEnumerator()
            => _store.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
