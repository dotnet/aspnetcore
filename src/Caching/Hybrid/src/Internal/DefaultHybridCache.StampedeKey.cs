// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal readonly struct StampedeKey : IEquatable<StampedeKey>
    {
        private readonly string _key;
        private readonly HybridCacheEntryFlags _flags;
        private readonly int _hashCode; // we know we'll need it; compute it once only
        public StampedeKey(string key, HybridCacheEntryFlags flags)
        {
            // We'll use both the key *and* the flags as combined flag; in reality, we *expect*
            // the flags to be consistent between calls on the same operation, and it must be
            // noted that the *cache items* only use the key (not the flags), but: it gets
            // very hard to grok what the correct behaviour should be if combining two calls
            // with different flags, since they could have mutually exclusive behaviours!

            // As such, we'll treat conflicting calls entirely separately from a stampede
            // perspective.
            _key = key;
            _flags = flags;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            _hashCode = System.HashCode.Combine(key, flags);
#else
            _hashCode = key.GetHashCode() ^ (int)flags;
#endif
        }

        public string Key => _key;
        public HybridCacheEntryFlags Flags => _flags;

        // allow direct access to the pre-computed hash-code, semantically emphasizing that
        // this is a constant-time operation against a known value
        internal int HashCode => _hashCode;

        public bool Equals(StampedeKey other) => _flags == other._flags & _key == other._key;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is StampedeKey other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public override string ToString() => $"{_key} ({_flags})";
    }
}
