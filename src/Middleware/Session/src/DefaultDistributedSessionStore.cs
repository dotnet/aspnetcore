// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Session;

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
