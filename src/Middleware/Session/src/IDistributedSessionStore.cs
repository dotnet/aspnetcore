// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Session;

internal interface IDistributedSessionStore : IEnumerable<KeyValuePair<EncodedKey, byte[]>>
{
    int Count { get; }

    ICollection<EncodedKey> Keys { get; }

    bool TryGetValue(EncodedKey key, [MaybeNullWhen(false)] out byte[] value);

    void SetValue(EncodedKey key, byte[] value);

    bool Remove(EncodedKey encodedKey);

    void Clear();
}
