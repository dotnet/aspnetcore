// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal readonly struct StampedeKey : IEquatable<StampedeKey>
    {
        private readonly string key;
        private readonly HybridCacheEntryFlags flags;
        private readonly int hashCode; // we know we'll need it; compute it once only
        public StampedeKey(string key, HybridCacheEntryFlags flags)
        {
            this.key = key;
            this.flags = flags;
            this.hashCode = key.GetHashCode() ^ (int)flags;
        }

        public bool Equals(StampedeKey other) => this.flags == other.flags & this.key == other.key;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is StampedeKey other && Equals(other);

        public override int GetHashCode() => hashCode;

        public override string ToString() => $"{key} ({flags})";
    }
}
