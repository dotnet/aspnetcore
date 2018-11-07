// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// An implementation of <see cref="IMemoryCache"/> using a dictionary to
    /// store its entries.
    /// </summary>
    public class MemoryCache : IMemoryCache
    {
        private readonly ConcurrentDictionary<object, CacheEntry> _entries;
        private long _cacheSize = 0;
        private bool _disposed;

        // We store the delegates locally to prevent allocations
        // every time a new CacheEntry is created.
        private readonly Action<CacheEntry> _setEntry;
        private readonly Action<CacheEntry> _entryExpirationNotification;

        private readonly MemoryCacheOptions _options;
        private DateTimeOffset _lastExpirationScan;

        /// <summary>
        /// Creates a new <see cref="MemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            _entries = new ConcurrentDictionary<object, CacheEntry>();
            _setEntry = SetEntry;
            _entryExpirationNotification = EntryExpired;

            if (_options.Clock == null)
            {
                _options.Clock = new SystemClock();
            }

            _lastExpirationScan = _options.Clock.UtcNow;
        }

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~MemoryCache()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count
        {
            get { return _entries.Count; }
        }

        // internal for testing
        internal long Size { get => Interlocked.Read(ref _cacheSize); }

        private ICollection<KeyValuePair<object, CacheEntry>> EntriesCollection => _entries;

        /// <inheritdoc />
        public ICacheEntry CreateEntry(object key)
        {
            CheckDisposed();

            ValidateCacheKey(key);

            return new CacheEntry(
                key,
                _setEntry,
                _entryExpirationNotification
            );
        }

        private void SetEntry(CacheEntry entry)
        {
            if (_disposed)
            {
                // No-op instead of throwing since this is called during CacheEntry.Dispose
                return;
            }

            if (_options.SizeLimit.HasValue && !entry.Size.HasValue)
            {
                throw new InvalidOperationException($"Cache entry must specify a value for {nameof(entry.Size)} when {nameof(_options.SizeLimit)} is set.");
            }

            var utcNow = _options.Clock.UtcNow;

            DateTimeOffset? absoluteExpiration = null;
            if (entry._absoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow + entry._absoluteExpirationRelativeToNow;
            }
            else if (entry._absoluteExpiration.HasValue)
            {
                absoluteExpiration = entry._absoluteExpiration;
            }

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (absoluteExpiration.HasValue)
            {
                if (!entry._absoluteExpiration.HasValue || absoluteExpiration.Value < entry._absoluteExpiration.Value)
                {
                    entry._absoluteExpiration = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            if (_entries.TryGetValue(entry.Key, out CacheEntry priorEntry))
            {
                priorEntry.SetExpired(EvictionReason.Replaced);
            }

            var exceedsCapacity = UpdateCacheSizeExceedsCapacity(entry);

            if (!entry.CheckExpired(utcNow) && !exceedsCapacity)
            {
                var entryAdded = false;

                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    entryAdded = _entries.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = _entries.TryUpdate(entry.Key, entry, priorEntry);

                    if (entryAdded)
                    {
                        if (_options.SizeLimit.HasValue)
                        {
                            // The prior entry was removed, decrease the by the prior entry's size
                            Interlocked.Add(ref _cacheSize, -priorEntry.Size.Value);
                        }
                    }
                    else
                    {
                        // The update will fail if the previous entry was removed after retrival.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        entryAdded = _entries.TryAdd(entry.Key, entry);
                    }
                }

                if (entryAdded)
                {
                    entry.AttachTokens();
                }
                else
                {
                    if (_options.SizeLimit.HasValue)
                    {
                        // Entry could not be added, reset cache size
                        Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                    }
                    entry.SetExpired(EvictionReason.Replaced);
                    entry.InvokeEvictionCallbacks();
                }

                if (priorEntry != null)
                {
                    priorEntry.InvokeEvictionCallbacks();
                }
            }
            else
            {
                if (exceedsCapacity)
                {
                    // The entry was not added due to overcapacity
                    entry.SetExpired(EvictionReason.Capacity);

                    TriggerOvercapacityCompaction();
                }

                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    RemoveEntry(priorEntry);
                }
            }

            StartScanForExpiredItems();
        }

        /// <inheritdoc />
        public bool TryGetValue(object key, out object result)
        {
            ValidateCacheKey(key);

            CheckDisposed();

            result = null;
            var utcNow = _options.Clock.UtcNow;
            var found = false;

            if (_entries.TryGetValue(key, out CacheEntry entry))
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to concurrent calls to SetExpired during SetEntry.
                if (entry.CheckExpired(utcNow) && entry.EvictionReason != EvictionReason.Replaced)
                {
                    // TODO: For efficiency queue this up for batch removal
                    RemoveEntry(entry);
                }
                else
                {
                    found = true;
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    // When this entry is retrieved in the scope of creating another entry,
                    // that entry needs a copy of these expiration tokens.
                    entry.PropagateOptions(CacheEntryHelper.Current);
                }
            }

            StartScanForExpiredItems();

            return found;
        }

        /// <inheritdoc />
        public void Remove(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            CheckDisposed();
            if (_entries.TryRemove(key, out CacheEntry entry))
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                }

                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItems();
        }

        private void RemoveEntry(CacheEntry entry)
        {
            if (EntriesCollection.Remove(new KeyValuePair<object, CacheEntry>(entry.Key, entry)))
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                }
                entry.InvokeEvictionCallbacks();
            }
        }

        private void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficiency consider processing these expirations in batches.
            RemoveEntry(entry);
            StartScanForExpiredItems();
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        private void StartScanForExpiredItems()
        {
            var now = _options.Clock.UtcNow;
            if (_options.ExpirationScanFrequency < now - _lastExpirationScan)
            {
                _lastExpirationScan = now;
                Task.Factory.StartNew(state => ScanForExpiredItems((MemoryCache)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void ScanForExpiredItems(MemoryCache cache)
        {
            var now = cache._options.Clock.UtcNow;
            foreach (var entry in cache._entries.Values)
            {
                if (entry.CheckExpired(now))
                {
                    cache.RemoveEntry(entry);
                }
            }
        }

        private bool UpdateCacheSizeExceedsCapacity(CacheEntry entry)
        {
            if (!_options.SizeLimit.HasValue)
            {
                return false;
            }

            var newSize = 0L;
            for (var i = 0; i < 100; i++)
            {
                var sizeRead = Interlocked.Read(ref _cacheSize);
                newSize = sizeRead + entry.Size.Value;

                if (newSize < 0 || newSize > _options.SizeLimit)
                {
                    // Overflow occured, return true without updating the cache size
                    return true;
                }

                if (sizeRead == Interlocked.CompareExchange(ref _cacheSize, newSize, sizeRead))
                {
                    return false;
                }
            }

            return true;
        }

        private void TriggerOvercapacityCompaction()
        {
            // Spawn background thread for compaction
            ThreadPool.QueueUserWorkItem(s => OvercapacityCompaction((MemoryCache)s), this);
        }

        private static void OvercapacityCompaction(MemoryCache cache)
        {
            var currentSize = Interlocked.Read(ref cache._cacheSize);
            var lowWatermark = cache._options.SizeLimit * (1 - cache._options.CompactionPercentage);
            if (currentSize > lowWatermark)
            {
                cache.Compact(currentSize - (long)lowWatermark, entry => entry.Size.Value);
            }
        }

        /// Remove at least the given percentage (0.10 for 10%) of the total entries (or estimated memory?), according to the following policy:
        /// 1. Remove all expired items.
        /// 2. Bucket by CacheItemPriority.
        /// 3. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        public void Compact(double percentage)
        {
            int removalCountTarget = (int)(_entries.Count * percentage);
            Compact(removalCountTarget, _ => 1);
        }

        private void Compact(long removalSizeTarget, Func<CacheEntry, long> computeEntrySize)
        {
            var entriesToRemove = new List<CacheEntry>();
            var lowPriEntries = new List<CacheEntry>();
            var normalPriEntries = new List<CacheEntry>();
            var highPriEntries = new List<CacheEntry>();
            long removedSize = 0;

            // Sort items by expired & priority status
            var now = _options.Clock.UtcNow;
            foreach (var entry in _entries.Values)
            {
                if (entry.CheckExpired(now))
                {
                    entriesToRemove.Add(entry);
                    removedSize += computeEntrySize(entry);
                }
                else
                {
                    switch (entry.Priority)
                    {
                        case CacheItemPriority.Low:
                            lowPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.NeverRemove:
                            break;
                        default:
                            throw new NotSupportedException("Not implemented: " + entry.Priority);
                    }
                }
            }

            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, lowPriEntries);
            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, normalPriEntries);
            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, highPriEntries);

            foreach (var entry in entriesToRemove)
            {
                RemoveEntry(entry);
            }
        }

        /// Policy:
        /// 1. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        private void ExpirePriorityBucket(ref long removedSize, long removalSizeTarget, Func<CacheEntry, long> computeEntrySize, List<CacheEntry> entriesToRemove, List<CacheEntry> priorityEntries)
        {
            // Do we meet our quota by just removing expired entries?
            if (removalSizeTarget <= removedSize)
            {
                // No-op, we've met quota
                return;
            }

            // Expire enough entries to reach our goal
            // TODO: Refine policy

            // LRU
            foreach (var entry in priorityEntries.OrderBy(entry => entry.LastAccessed))
            {
                entry.SetExpired(EvictionReason.Capacity);
                entriesToRemove.Add(entry);
                removedSize += computeEntrySize(entry);

                if (removalSizeTarget <= removedSize)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                _disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(MemoryCache).FullName);
            }
        }

        private static void ValidateCacheKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}