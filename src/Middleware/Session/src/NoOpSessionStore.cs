// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Session
{
    internal class NoOpSessionStore : IDistributedSessionStore
    {
        public void SetValue(EncodedKey key, byte[] value)
        {
        }

        public int Count => 0;

        public bool IsReadOnly { get; }

        public ICollection<EncodedKey> Keys { get; } = Array.Empty<EncodedKey>();

        public ICollection<byte[]> Values { get; } = new byte[0][];

        public void Clear() { }

        public IEnumerator<KeyValuePair<EncodedKey, byte[]>> GetEnumerator() => Enumerable.Empty<KeyValuePair<EncodedKey, byte[]>>().GetEnumerator();

        public bool Remove(EncodedKey key) => false;

        public bool TryGetValue(EncodedKey key, [MaybeNullWhen(false)] out byte[] value)
        {
            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
