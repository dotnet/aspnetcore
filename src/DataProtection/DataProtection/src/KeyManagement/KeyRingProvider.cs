// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

internal sealed class KeyRingProvider : ICacheableKeyRingProvider, IKeyRingProvider
{
    private const string DisableAsyncKeyRingUpdateSwitchKey = "Microsoft.AspNetCore.DataProtection.KeyManagement.DisableAsyncKeyRingUpdate";

    private CacheableKeyRing? _cacheableKeyRing;
    private readonly object _cacheableKeyRingLockObj = new object();
    private Task<CacheableKeyRing>? _cacheableKeyRingTask; // Also covered by _cacheableKeyRingLockObj
    private readonly IDefaultKeyResolver _defaultKeyResolver;
    private readonly bool _autoGenerateKeys;
    private readonly TimeSpan _newKeyLifetime;
    private readonly IKeyManager _keyManager;
    private readonly ILogger _logger;
    private readonly bool _disableAsyncKeyRingUpdate;

    public KeyRingProvider(
        IKeyManager keyManager,
        IOptions<KeyManagementOptions> keyManagementOptions,
        IDefaultKeyResolver defaultKeyResolver)
        : this(
              keyManager,
              keyManagementOptions,
              defaultKeyResolver,
              NullLoggerFactory.Instance)
    {
    }

    public KeyRingProvider(
        IKeyManager keyManager,
        IOptions<KeyManagementOptions> keyManagementOptions,
        IDefaultKeyResolver defaultKeyResolver,
        ILoggerFactory loggerFactory)
    {
        var options = keyManagementOptions.Value ?? new();
        _autoGenerateKeys = options.AutoGenerateKeys;
        _newKeyLifetime = options.NewKeyLifetime;
        _keyManager = keyManager;
        CacheableKeyRingProvider = this;
        _defaultKeyResolver = defaultKeyResolver;
        _logger = loggerFactory.CreateLogger<KeyRingProvider>();

        // We will automatically refresh any unknown keys for 2 minutes see https://github.com/dotnet/aspnetcore/issues/3975
        AutoRefreshWindowEnd = DateTime.UtcNow.AddMinutes(2);

        AppContext.TryGetSwitch(DisableAsyncKeyRingUpdateSwitchKey, out _disableAsyncKeyRingUpdate);

        // We use the Random class since we don't need a secure PRNG for this.
#if NET6_0_OR_GREATER
        JitterRandom = Random.Shared;
#else
        JitterRandom = new Random();
#endif
    }

    // Internal for testing
    internal Random JitterRandom { get; set; }

    // for testing
    internal ICacheableKeyRingProvider CacheableKeyRingProvider { get; set; }

    internal DateTime AutoRefreshWindowEnd { get; set; }

    internal bool InAutoRefreshWindow() => DateTime.UtcNow < AutoRefreshWindowEnd;

    private CacheableKeyRing CreateCacheableKeyRingCore(DateTimeOffset now, IKey? keyJustAdded)
    {
        // Refresh the list of all keys
        var cacheExpirationToken = _keyManager.GetCacheExpirationToken();
        var allKeys = _keyManager.GetAllKeys();

        // Fetch the current default key from the list of all keys
        var defaultKeyPolicy = _defaultKeyResolver.ResolveDefaultKeyPolicy(now, allKeys);
        var defaultKey = defaultKeyPolicy.DefaultKey;

        // We shouldn't call CreateKey more than once, else we risk stack diving. Thus, we don't even
        // check defaultKeyPolicy.ShouldGenerateNewKey.  However, this code path shouldn't get hit
        // with ShouldGenerateNewKey true unless there was an ineligible key with an activation date
        // slightly later than the one we just added. If this does happen, then we'll just use whatever
        // key we can instead of creating new keys endlessly, eventually falling back to the one we just
        // added if all else fails.
        if (keyJustAdded != null)
        {
            var keyToUse = defaultKey ?? defaultKeyPolicy.FallbackKey ?? keyJustAdded;
            return CreateCacheableKeyRingCoreStep2(now, cacheExpirationToken, keyToUse, allKeys);
        }

        // Determine whether we need to generate a new key
        bool shouldGenerateNewKey;
        if (defaultKeyPolicy.ShouldGenerateNewKey || defaultKey == null)
        {
            shouldGenerateNewKey = true;
        }
        else
        {
            // If we have a default key, we have to consider its expiration date.  We have to generate a replacement
            // if it will expire within the propagation window starting now (so that all other consumers pick up the
            // replacement before the current default key expires).  However, we also have to factor in the refresh
            // period, since we need to ensure that key generation occurs during the refresh that *precedes* the
            // propagation window ending at the expiration date.
            var minExpirationDate = now + KeyManagementOptions.KeyRingRefreshPeriod + KeyManagementOptions.KeyPropagationWindow;
            var defaultKeyExpirationDate = defaultKey.ExpirationDate;
            shouldGenerateNewKey =
                defaultKeyExpirationDate < minExpirationDate &&
                    (_defaultKeyResolver.ResolveDefaultKeyPolicy(defaultKeyExpirationDate, allKeys).DefaultKey is not { } nextDefaultKey ||
                    nextDefaultKey.ExpirationDate < minExpirationDate);
        }

        if (!shouldGenerateNewKey)
        {
            CryptoUtil.Assert(defaultKey != null, "Expected to see a default key.");
            return CreateCacheableKeyRingCoreStep2(now, cacheExpirationToken, defaultKey, allKeys);
        }

        _logger.PolicyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing();

        // At this point, we know we need to generate a new key.

        // We have been asked to generate a new key, but auto-generation of keys has been disabled.
        // We need to use the fallback key or fail.
        if (!_autoGenerateKeys)
        {
            var keyToUse = defaultKey ?? defaultKeyPolicy.FallbackKey;
            if (keyToUse == null)
            {
                _logger.KeyRingDoesNotContainValidDefaultKey();
                throw new InvalidOperationException(Resources.KeyRingProvider_NoDefaultKey_AutoGenerateDisabled);
            }
            else
            {
                _logger.UsingFallbackKeyWithExpirationAsDefaultKey(keyToUse.KeyId, keyToUse.ExpirationDate);
                return CreateCacheableKeyRingCoreStep2(now, cacheExpirationToken, keyToUse, allKeys);
            }
        }

        // We're going to generate a new key.  You'd think we could just take for granted what effect
        // this would have on the final result, but the key resolver is an extension point, so we have
        // to give it a chance to weigh in - hence the recursive call, triggering re-resolution.
        if (defaultKey == null)
        {
            // The case where there's no default key is the easiest scenario, since it
            // means that we need to create a new key with immediate activation.
            var newKey = _keyManager.CreateNewKey(activationDate: now, expirationDate: now + _newKeyLifetime);
            return CreateCacheableKeyRingCore(now, keyJustAdded: newKey); // recursively call
        }
        else
        {
            // If there is a default key, then the new key we generate should become active upon
            // expiration of the default key. The new key lifetime is measured from the creation
            // date (now), not the activation date.
            var newKey = _keyManager.CreateNewKey(activationDate: defaultKey.ExpirationDate, expirationDate: now + _newKeyLifetime);
            return CreateCacheableKeyRingCore(now, keyJustAdded: newKey); // recursively call
        }
    }

    private CacheableKeyRing CreateCacheableKeyRingCoreStep2(DateTimeOffset now, CancellationToken cacheExpirationToken, IKey defaultKey, IEnumerable<IKey> allKeys)
    {
        Debug.Assert(defaultKey != null);

        // Invariant: our caller ensures that CreateEncryptorInstance succeeded at least once
        Debug.Assert(defaultKey.CreateEncryptor() != null);

        // This can happen if there's a date-based revocation that's in the future (e.g. because of clock skew)
        if (defaultKey.IsRevoked)
        {
            _logger.KeyRingDefaultKeyIsRevoked(defaultKey.KeyId);
            throw Error.KeyRingProvider_DefaultKeyRevoked(defaultKey.KeyId);
        }

        _logger.UsingKeyAsDefaultKey(defaultKey.KeyId);

        var nextAutoRefreshTime = now + GetRefreshPeriodWithJitter(KeyManagementOptions.KeyRingRefreshPeriod);

        // The cached keyring should expire at the earliest of (default key expiration, next auto-refresh time).
        // Since the refresh period and safety window are not user-settable, we can guarantee that there's at
        // least one auto-refresh between the start of the safety window and the key's expiration date.
        // This gives us an opportunity to update the key ring before expiration, and it prevents multiple
        // servers in a cluster from trying to update the key ring simultaneously. Special case: if the default
        // key's expiration date is in the past, then we know we're using a fallback key and should disregard
        // its expiration date in favor of the next auto-refresh time.
        return new CacheableKeyRing(
            expirationToken: cacheExpirationToken,
            expirationTime: (defaultKey.ExpirationDate <= now) ? nextAutoRefreshTime : Min(defaultKey.ExpirationDate, nextAutoRefreshTime),
            defaultKey: defaultKey,
            allKeys: allKeys);
    }

    public IKeyRing GetCurrentKeyRing()
    {
        return GetCurrentKeyRingCore(DateTime.UtcNow);
    }

    internal IKeyRing RefreshCurrentKeyRing()
    {
        return GetCurrentKeyRingCore(DateTime.UtcNow, forceRefresh: true);
    }

    internal IKeyRing GetCurrentKeyRingCore(DateTime utcNow, bool forceRefresh = false)
    {
        // We're making a big, scary change to the way this cache is updated: now threads
        // only block during computation of the new value if no old value is available
        // (or if they force it).  We'll leave the old code in place, behind an appcontext
        // switch in case it turns out to have unwelcome emergent behavior.
        // TODO: Delete one of these codepaths in 10.0.
        return _disableAsyncKeyRingUpdate
            ? GetCurrentKeyRingCoreOld(utcNow, forceRefresh)
            : GetCurrentKeyRingCoreNew(utcNow, forceRefresh);
    }

    private IKeyRing GetCurrentKeyRingCoreOld(DateTime utcNow, bool forceRefresh)
    {
        // DateTimes are only meaningfully comparable if they share the same Kind - require Utc for consistency
        Debug.Assert(utcNow.Kind == DateTimeKind.Utc);

        // Can we return the cached keyring to the caller?
        CacheableKeyRing? existingCacheableKeyRing = null;
        if (!forceRefresh)
        {
            existingCacheableKeyRing = Volatile.Read(ref _cacheableKeyRing);
            if (CacheableKeyRing.IsValid(existingCacheableKeyRing, utcNow))
            {
                return existingCacheableKeyRing.KeyRing;
            }
        }

        // The cached keyring hasn't been created or must be refreshed. We'll allow one thread to
        // update the keyring, and all other threads will continue to use the existing cached
        // keyring while the first thread performs the update. There is an exception: if there
        // is no usable existing cached keyring, all callers must block until the keyring exists.
        var acquiredLock = false;
        try
        {
            Monitor.TryEnter(_cacheableKeyRingLockObj, (existingCacheableKeyRing != null) ? 0 : Timeout.Infinite, ref acquiredLock);
            if (acquiredLock)
            {
                if (!forceRefresh)
                {
                    // This thread acquired the critical section and is responsible for updating the
                    // cached keyring. But first, let's make sure that somebody didn't sneak in before
                    // us and update the keyring on our behalf.
                    existingCacheableKeyRing = Volatile.Read(ref _cacheableKeyRing);
                    if (CacheableKeyRing.IsValid(existingCacheableKeyRing, utcNow))
                    {
                        return existingCacheableKeyRing.KeyRing;
                    }

                    if (existingCacheableKeyRing != null)
                    {
                        _logger.ExistingCachedKeyRingIsExpired();
                    }
                }

                // It's up to us to refresh the cached keyring.
                // This call is performed *under lock*.
                CacheableKeyRing newCacheableKeyRing;

                try
                {
                    newCacheableKeyRing = CacheableKeyRingProvider.GetCacheableKeyRing(utcNow);
                }
                catch (Exception ex)
                {
                    if (existingCacheableKeyRing != null)
                    {
                        _logger.ErrorOccurredWhileRefreshingKeyRing(ex);
                    }
                    else
                    {
                        _logger.ErrorOccurredWhileReadingKeyRing(ex);
                    }

                    // Failures that occur while refreshing the keyring are most likely transient, perhaps due to a
                    // temporary network outage. Since we don't want every subsequent call to result in failure, we'll
                    // create a new keyring object whose expiration is now + some short period of time (currently 2 min),
                    // and after this period has elapsed the next caller will try refreshing. If we don't have an
                    // existing keyring (perhaps because this is the first call), then there's nothing to extend, so
                    // each subsequent caller will keep going down this code path until one succeeds.
                    if (existingCacheableKeyRing != null)
                    {
                        Volatile.Write(ref _cacheableKeyRing, existingCacheableKeyRing.WithTemporaryExtendedLifetime(utcNow));
                    }

                    // The immediate caller should fail so that they can report the error up the chain. This makes it more likely
                    // that an administrator can see the error and react to it as appropriate. The caller can retry the operation
                    // and will probably have success as long as they fall within the temporary extension mentioned above.
                    throw;
                }

                Volatile.Write(ref _cacheableKeyRing, newCacheableKeyRing);
                return newCacheableKeyRing.KeyRing;
            }
            else
            {
                // We didn't acquire the critical section. This should only occur if we passed
                // zero for the Monitor.TryEnter timeout, which implies that we had an existing
                // (but outdated) keyring that we can use as a fallback.
                Debug.Assert(existingCacheableKeyRing != null);
                return existingCacheableKeyRing.KeyRing;
            }
        }
        finally
        {
            if (acquiredLock)
            {
                Monitor.Exit(_cacheableKeyRingLockObj);
            }
        }
    }

    private IKeyRing GetCurrentKeyRingCoreNew(DateTime utcNow, bool forceRefresh)
    {
        // DateTimes are only meaningfully comparable if they share the same Kind - require Utc for consistency
        Debug.Assert(utcNow.Kind == DateTimeKind.Utc);

        // The 99% and perf-critical case is that there is no task in-flight and the cached
        // key ring is valid.  We do what we can to avoid unnecessary overhead (locking,
        // context switching, etc) on this path.

        // Can we return the cached keyring to the caller?
        if (!forceRefresh)
        {
            var cached = Volatile.Read(ref _cacheableKeyRing);
            if (CacheableKeyRing.IsValid(cached, utcNow))
            {
                return cached.KeyRing;
            }
        }

        CacheableKeyRing? existingCacheableKeyRing = null;
        Task<CacheableKeyRing>? existingTask = null;

        lock (_cacheableKeyRingLockObj)
        {
            // Did another thread acquire the lock first and populate the cache?
            // This could have happened if there was a completed in-flight task for the other thread to process.
            if (!forceRefresh)
            {
                existingCacheableKeyRing = Volatile.Read(ref _cacheableKeyRing);
                if (CacheableKeyRing.IsValid(existingCacheableKeyRing, utcNow))
                {
                    return existingCacheableKeyRing.KeyRing;
                }
            }

            existingTask = _cacheableKeyRingTask;
            if (existingTask is null)
            {
                // If there's no existing task, make one now
                // PERF: Closing over utcNow substantially slows down the fast case (valid cache) in micro-benchmarks
                // (closing over `this` for CacheableKeyRingProvider doesn't seem impactful)
                existingTask = Task.Factory.StartNew(
                    utcNowState => CacheableKeyRingProvider.GetCacheableKeyRing((DateTime)utcNowState!),
                    utcNow,
                    CancellationToken.None, // GetKeyRingFromCompletedTaskUnsynchronized will need to react if this becomes cancellable
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
                _cacheableKeyRingTask = existingTask;
            }

            // This is mostly for the case where existingTask already set, but no harm in checking a fresh one
            if (existingTask.IsCompleted)
            {
                // If work kicked off by a previous caller has completed, we should use those results.
                // Logically, it would probably make more sense to check this before checking whether
                // the cache is valid - there could be a newer value available - but keeping that path
                // fast is more important.  The next forced refresh or cache expiration will cause the
                // new value to be picked up.

                // An unconsumed task result is considered to satisfy forceRefresh.  One could quibble that this isn't really
                // a forced refresh, but we'll still return a key ring newer than the one the caller was dissatisfied with.
                var taskKeyRing = GetKeyRingFromCompletedTaskUnsynchronized(existingTask, utcNow); // Throws if the task failed
                Debug.Assert(taskKeyRing is not null, "How did _cacheableKeyRingTask change while we were holding the lock?");
                return taskKeyRing;
            }
        }

        // Prefer a stale cached key ring to blocking
        if (existingCacheableKeyRing is not null)
        {
            Debug.Assert(!forceRefresh, "Consumed cached key ring even though forceRefresh is true");
            Debug.Assert(!CacheableKeyRing.IsValid(existingCacheableKeyRing, utcNow), "Should have returned a valid cached key ring above");
            return existingCacheableKeyRing.KeyRing;
        }

        // If there's not even a stale cached key ring we can use, we have to wait.
        // It's not ideal to wait for a task that was just scheduled, but it makes the code a lot simpler
        // (compared to having a separate, synchronous code path).

        // The reason we yield the lock and wait for the task instead is to allow racing forceRefresh threads
        // to wait for the same task, rather than being sequentialized (and each doing its own refresh).

        // Cleverness: swallow any exceptions - they'll be surfaced by GetKeyRingFromCompletedTaskUnsynchronized, if appropriate.
        existingTask
            .ContinueWith(
                static t => _ = t.Exception, // Still observe the exception - just don't throw it
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default)
            .Wait();

        lock (_cacheableKeyRingLockObj)
        {
            var newKeyRing = GetKeyRingFromCompletedTaskUnsynchronized(existingTask, utcNow); // Throws if the task failed (winning thread only)
            if (newKeyRing is null)
            {
                // Another thread won - check whether it cached a new key ring
                var newCacheableKeyRing = Volatile.Read(ref _cacheableKeyRing);
                if (newCacheableKeyRing is null)
                {
                    // There will have been a better exception from the winning thread
                    throw Error.KeyRingProvider_RefreshFailedOnOtherThread(existingTask.Exception);
                }

                newKeyRing = newCacheableKeyRing.KeyRing;
            }

            return newKeyRing;
        }
    }

    /// <summary>
    /// If the given completed task completed successfully, clears the task and either
    /// caches and returns the resulting key ring or throws, according to the successfulness
    /// of the task.
    /// </summary>
    /// <remarks>
    /// Must be called under <see cref="_cacheableKeyRingLockObj"/>.
    /// </remarks>
    private IKeyRing? GetKeyRingFromCompletedTaskUnsynchronized(Task<CacheableKeyRing> task, DateTime utcNow)
    {
        Debug.Assert(task.IsCompleted);
        Debug.Assert(!task.IsCanceled, "How did a task with no cancellation token get canceled?");

        // If the parameter doesn't match the field, another thread has already consumed the task (and it's reflected in _cacheableKeyRing)
        if (!ReferenceEquals(task, _cacheableKeyRingTask))
        {
            return null;
        }

        _cacheableKeyRingTask = null;

        try
        {
            var newCacheableKeyRing = task.GetAwaiter().GetResult(); // Call GetResult to throw on failure
            Volatile.Write(ref _cacheableKeyRing, newCacheableKeyRing);
            return newCacheableKeyRing.KeyRing;
        }
        catch (Exception e)
        {
            var existingCacheableKeyRing = Volatile.Read(ref _cacheableKeyRing);
            if (existingCacheableKeyRing is not null && !CacheableKeyRing.IsValid(existingCacheableKeyRing, utcNow))
            {
                // If reading failed, we probably don't want to try again for a little bit, so slightly extend the
                // lifetime of the current cache entry
                Volatile.Write(ref _cacheableKeyRing, existingCacheableKeyRing.WithTemporaryExtendedLifetime(utcNow));

                _logger.ErrorOccurredWhileRefreshingKeyRing(e); // This one mentions the no-retry window
            }
            else
            {
                _logger.ErrorOccurredWhileReadingKeyRing(e);
            }

            throw;
        }
    }

    private TimeSpan GetRefreshPeriodWithJitter(TimeSpan refreshPeriod)
    {
        // We'll fudge the refresh period up to -20% so that multiple applications don't try to
        // hit a single repository simultaneously. For instance, if the refresh period is 1 hour,
        // we'll return a value in the vicinity of 48 - 60 minutes.
        return TimeSpan.FromTicks((long)(refreshPeriod.Ticks * (1.0d - (JitterRandom.NextDouble() / 5))));
    }

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b)
    {
        return (a < b) ? a : b;
    }

    CacheableKeyRing ICacheableKeyRingProvider.GetCacheableKeyRing(DateTimeOffset now)
    {
        // the entry point allows one recursive call
        return CreateCacheableKeyRingCore(now, keyJustAdded: null);
    }
}
