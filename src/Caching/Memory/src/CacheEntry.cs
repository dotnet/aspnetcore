// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntry : ICacheEntry
    {
        private bool _added = false;
        private static readonly Action<object> ExpirationCallback = ExpirationTokensExpired;
        private readonly Action<CacheEntry> _notifyCacheOfExpiration;
        private readonly Action<CacheEntry> _notifyCacheEntryDisposed;
        private IList<IDisposable> _expirationTokenRegistrations;
        private IList<PostEvictionCallbackRegistration> _postEvictionCallbacks;
        private bool _isExpired;

        internal IList<IChangeToken> _expirationTokens;
        internal DateTimeOffset? _absoluteExpiration;
        internal TimeSpan? _absoluteExpirationRelativeToNow;
        private TimeSpan? _slidingExpiration;
        private long? _size;
        private IDisposable _scope;

        internal readonly object _lock = new object();

        internal CacheEntry(
            object key,
            Action<CacheEntry> notifyCacheEntryDisposed,
            Action<CacheEntry> notifyCacheOfExpiration)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (notifyCacheEntryDisposed == null)
            {
                throw new ArgumentNullException(nameof(notifyCacheEntryDisposed));
            }

            if (notifyCacheOfExpiration == null)
            {
                throw new ArgumentNullException(nameof(notifyCacheOfExpiration));
            }

            Key = key;
            _notifyCacheEntryDisposed = notifyCacheEntryDisposed;
            _notifyCacheOfExpiration = notifyCacheOfExpiration;

            _scope = CacheEntryHelper.EnterScope(this);
        }

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration
        {
            get
            {
                return _absoluteExpiration;
            }
            set
            {
                _absoluteExpiration = value;
            }
        }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get
            {
                return _absoluteExpirationRelativeToNow;
            }
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(AbsoluteExpirationRelativeToNow),
                        value,
                        "The relative expiration value must be positive.");
                }

                _absoluteExpirationRelativeToNow = value;
            }
        }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get
            {
                return _slidingExpiration;
            }
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(SlidingExpiration),
                        value,
                        "The sliding expiration value must be positive.");
                }
                _slidingExpiration = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> instances which cause the cache entry to expire.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens
        {
            get
            {
                if (_expirationTokens == null)
                {
                    _expirationTokens = new List<IChangeToken>();
                }

                return _expirationTokens;
            }
        }

        /// <summary>
        /// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
        /// </summary>
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks
        {
            get
            {
                if (_postEvictionCallbacks == null)
                {
                    _postEvictionCallbacks = new List<PostEvictionCallbackRegistration>();
                }

                return _postEvictionCallbacks;
            }
        }

        /// <summary>
        /// Gets or sets the priority for keeping the cache entry in the cache during a
        /// memory pressure triggered cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
        /// </summary>
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

        /// <summary>
        /// Gets or sets the size of the cache entry value.
        /// </summary>
        public long? Size
        {
            get => _size;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                _size = value;
            }
        }

        public object Key { get; private set; }

        public object Value { get; set; }

        internal DateTimeOffset LastAccessed { get; set; }

        internal EvictionReason EvictionReason { get; private set; }

        public void Dispose()
        {
            if (!_added)
            {
                _added = true;
                _scope.Dispose();
                _notifyCacheEntryDisposed(this);
                PropagateOptions(CacheEntryHelper.Current);
            }
        }

        internal bool CheckExpired(DateTimeOffset now)
        {
            return _isExpired || CheckForExpiredTime(now) || CheckForExpiredTokens();
        }

        internal void SetExpired(EvictionReason reason)
        {
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            _isExpired = true;
            DetachTokens();
        }

        private bool CheckForExpiredTime(DateTimeOffset now)
        {
            if (_absoluteExpiration.HasValue && _absoluteExpiration.Value <= now)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            if (_slidingExpiration.HasValue
                && (now - LastAccessed) >= _slidingExpiration)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            return false;
        }

        internal bool CheckForExpiredTokens()
        {
            if (_expirationTokens != null)
            {
                for (int i = 0; i < _expirationTokens.Count; i++)
                {
                    var expiredToken = _expirationTokens[i];
                    if (expiredToken.HasChanged)
                    {
                        SetExpired(EvictionReason.TokenExpired);
                        return true;
                    }
                }
            }
            return false;
        }

        internal void AttachTokens()
        {
            if (_expirationTokens != null)
            {
                lock (_lock)
                {
                    for (int i = 0; i < _expirationTokens.Count; i++)
                    {
                        var expirationToken = _expirationTokens[i];
                        if (expirationToken.ActiveChangeCallbacks)
                        {
                            if (_expirationTokenRegistrations == null)
                            {
                                _expirationTokenRegistrations = new List<IDisposable>(1);
                            }
                            var registration = expirationToken.RegisterChangeCallback(ExpirationCallback, this);
                            _expirationTokenRegistrations.Add(registration);
                        }
                    }
                }
            }
        }

        private static void ExpirationTokensExpired(object obj)
        {
            // start a new thread to avoid issues with callbacks called from RegisterChangeCallback
            Task.Factory.StartNew(state =>
            {
                var entry = (CacheEntry)state;
                entry.SetExpired(EvictionReason.TokenExpired);
                entry._notifyCacheOfExpiration(entry);
            }, obj, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void DetachTokens()
        {
            lock(_lock)
            {
                var registrations = _expirationTokenRegistrations;
                if (registrations != null)
                {
                    _expirationTokenRegistrations = null;
                    for (int i = 0; i < registrations.Count; i++)
                    {
                        var registration = registrations[i];
                        registration.Dispose();
                    }
                }
            }
        }

        internal void InvokeEvictionCallbacks()
        {
            if (_postEvictionCallbacks != null)
            {
                Task.Factory.StartNew(state => InvokeCallbacks((CacheEntry)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void InvokeCallbacks(CacheEntry entry)
        {
            var callbackRegistrations = Interlocked.Exchange(ref entry._postEvictionCallbacks, null);

            if (callbackRegistrations == null)
            {
                return;
            }

            for (int i = 0; i < callbackRegistrations.Count; i++)
            {
                var registration = callbackRegistrations[i];

                try
                {
                    registration.EvictionCallback?.Invoke(entry.Key, entry.Value, entry.EvictionReason, registration.State);
                }
                catch (Exception)
                {
                    // This will be invoked on a background thread, don't let it throw.
                    // TODO: LOG
                }
            }
        }

        internal void PropagateOptions(CacheEntry parent)
        {
            if (parent == null)
            {
                return;
            }

            // Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
            // We do this regardless of it gets cached because the tokens are associated with the value we'll return.
            if (_expirationTokens != null)
            {
                lock (_lock)
                {
                    lock (parent._lock)
                    {
                        foreach (var expirationToken in _expirationTokens)
                        {
                            parent.AddExpirationToken(expirationToken);
                        }
                    }
                }
            }

            if (_absoluteExpiration.HasValue)
            {
                if (!parent._absoluteExpiration.HasValue || _absoluteExpiration < parent._absoluteExpiration)
                {
                    parent._absoluteExpiration = _absoluteExpiration;
                }
            }
        }
    }
}
