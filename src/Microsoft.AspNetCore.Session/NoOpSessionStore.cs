// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Session
{
    internal class NoOpSessionStore : IDictionary<EncodedKey, byte[]>
    {
        public byte[] this[EncodedKey key]
        {
            get
            {
                return null;
            }

            set
            {

            }
        }

        public int Count { get; } = 0;

        public bool IsReadOnly { get; } = false;

        public ICollection<EncodedKey> Keys { get; } = new EncodedKey[0];

        public ICollection<byte[]> Values { get; } = new byte[0][];

        public void Add(KeyValuePair<EncodedKey, byte[]> item) { }

        public void Add(EncodedKey key, byte[] value) { }

        public void Clear() { }

        public bool Contains(KeyValuePair<EncodedKey, byte[]> item) => false;

        public bool ContainsKey(EncodedKey key) => false;

        public void CopyTo(KeyValuePair<EncodedKey, byte[]>[] array, int arrayIndex) { }

        public IEnumerator<KeyValuePair<EncodedKey, byte[]>> GetEnumerator() => Enumerable.Empty<KeyValuePair<EncodedKey, byte[]>>().GetEnumerator();

        public bool Remove(KeyValuePair<EncodedKey, byte[]> item) => false;

        public bool Remove(EncodedKey key) => false;

        public bool TryGetValue(EncodedKey key, out byte[] value)
        {
            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
