// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    /// <summary>
    /// Wraps both a keyring and its expiration policy.
    /// </summary>
    internal sealed class CacheableKeyRing
    {
        private readonly CancellationToken _expirationToken;

        internal CacheableKeyRing(CancellationToken expirationToken, DateTimeOffset expirationTime, IKey defaultKey, IEnumerable<IKey> allKeys)
            : this(expirationToken, expirationTime, keyRing: new KeyRing(defaultKey, allKeys))
        {
        }

        internal CacheableKeyRing(CancellationToken expirationToken, DateTimeOffset expirationTime, IKeyRing keyRing)
        {
            _expirationToken = expirationToken;
            ExpirationTimeUtc = expirationTime.UtcDateTime;
            KeyRing = keyRing;
        }

        internal DateTime ExpirationTimeUtc { get; }

        internal IKeyRing KeyRing { get; }

        internal static bool IsValid(CacheableKeyRing keyRing, DateTime utcNow)
        {
            return keyRing != null
                && !keyRing._expirationToken.IsCancellationRequested
                && keyRing.ExpirationTimeUtc > utcNow;
        }
    }
}
