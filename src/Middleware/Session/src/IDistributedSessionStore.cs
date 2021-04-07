// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Session
{
    internal interface IDistributedSessionStore : IEnumerable<KeyValuePair<EncodedKey, byte[]>>
    {
        int Count { get; }

        ICollection<EncodedKey> Keys { get; }

        bool TryGetValue(EncodedKey key, [MaybeNullWhen(false)] out byte[] value);

        void SetValue(EncodedKey key, byte[] value);

        bool Remove(EncodedKey encodedKey);

        void Clear();
    }
}
