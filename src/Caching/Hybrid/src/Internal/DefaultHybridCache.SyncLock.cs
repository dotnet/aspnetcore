// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    // HybridCache's stampede protection requires some level of synchronization to avoid unnecessary runs
    // of the underlying data fetch; this is *minimized* by the use of double-checked locking and
    // interlocked join (adding a new request to an existing execution), but: that would leave a race
    // condition where the *remove* step of the stampede would be in a race with the *add new* step; the
    // *add new* step is inside a lock, but we need to *remove* step to share that lock, to avoid
    // the race. We deal with that by taking the same lock during remove, but *that* means we're locking
    // on all executions.
    //
    // To minimize lock contention, we will therefore use partitioning of the lock-token, by using the
    // low 3 bits of the hash-code (which we calculate eagerly only once, so: already known). This gives
    // us a fast way to split contention by 8, almost an order-of-magnitude, which is sufficient. We *could*
    // use an array for this, but: for directness, let's inline it instead (avoiding bounds-checks,
    // an extra layer of dereferencing, and the allocation; I will acknowledge these are miniscule, but:
    // it costs us nothing to do)

    private readonly object _syncLock0 = new(), _syncLock1 = new(), _syncLock2 = new(), _syncLock3 = new(),
        _syncLock4 = new(), _syncLock5 = new(), _syncLock6 = new(), _syncLock7 = new();

    internal object GetPartitionedSyncLock(in StampedeKey key)
        => (key.HashCode & 0b111) switch // generate 8 partitions using the low 3 bits
        {
            0 => _syncLock0, 1 => _syncLock1,
            2 => _syncLock2, 3 => _syncLock3,
            4 => _syncLock4, 5 => _syncLock5,
            6 => _syncLock6, _ => _syncLock7,
        };
}
