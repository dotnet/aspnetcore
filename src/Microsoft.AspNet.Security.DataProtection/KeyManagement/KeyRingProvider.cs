// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection.KeyManagement
{
    internal sealed class KeyRingProvider : IKeyRingProvider
    {
        // TODO: Should the below be 3 months?
        private static readonly TimeSpan KEY_DEFAULT_LIFETIME = TimeSpan.FromDays(30 * 6); // how long should keys be active once created?
        private static readonly TimeSpan KEYRING_REFRESH_PERIOD = TimeSpan.FromDays(1); // how often should we check for updates to the repository?
        private static readonly TimeSpan KEY_EXPIRATION_BUFFER = TimeSpan.FromDays(7); // how close to key expiration should we generate a new key?
        private static readonly TimeSpan MAX_SERVER_TO_SERVER_CLOCK_SKEW = TimeSpan.FromMinutes(10); // max skew we expect to see between servers using the key ring

        private CachedKeyRing _cachedKeyRing;
        private readonly object _cachedKeyRingLockObj = new object();
        private readonly IKeyManager _keyManager;

        public KeyRingProvider(IKeyManager keyManager)
        {
            _keyManager = keyManager;
        }

        private CachedKeyRing CreateCachedKeyRingInstanceUnderLock(DateTime utcNow, CachedKeyRing existingCachedKeyRing)
        {
            bool shouldCreateNewKeyWithDeferredActivation; // flag stating whether the default key will soon expire and doesn't have a suitable replacement

            // Must we discard the cached keyring and refresh directly from the manager?
            if (existingCachedKeyRing != null && existingCachedKeyRing.HardRefreshTimeUtc <= utcNow)
            {
                existingCachedKeyRing = null;
            }

            // Try to locate the current default key, using the cached keyring if we can.
            IKey defaultKey;
            if (existingCachedKeyRing != null)
            {
                defaultKey = FindDefaultKey(utcNow, existingCachedKeyRing.Keys, out shouldCreateNewKeyWithDeferredActivation);
                if (defaultKey != null && !shouldCreateNewKeyWithDeferredActivation)
                {
                    return new CachedKeyRing
                    {
                        KeyRing = new KeyRing(defaultKey.KeyId, existingCachedKeyRing.KeyRing), // this overload allows us to use existing IAuthenticatedEncryptor instances
                        Keys = existingCachedKeyRing.Keys,
                        HardRefreshTimeUtc = existingCachedKeyRing.HardRefreshTimeUtc,
                        SoftRefreshTimeUtc = MinDateTime(existingCachedKeyRing.HardRefreshTimeUtc, utcNow + KEYRING_REFRESH_PERIOD)
                    };
                }
            }

            // That didn't work, so refresh from the underlying key manager.
            var allKeys = _keyManager.GetAllKeys().ToArray();
            defaultKey = FindDefaultKey(utcNow, allKeys, out shouldCreateNewKeyWithDeferredActivation);

            if (defaultKey != null && shouldCreateNewKeyWithDeferredActivation)
            {
                // If we need to create a new key with deferred activation, do so now.
                _keyManager.CreateNewKey(activationDate: defaultKey.ExpirationDate, expirationDate: utcNow + KEY_DEFAULT_LIFETIME);
                allKeys = _keyManager.GetAllKeys().ToArray();
                defaultKey = FindDefaultKey(utcNow, allKeys);
            }
            else if (defaultKey == null)
            {
                // If there's no default key, create one now with immediate activation.
                _keyManager.CreateNewKey(utcNow, utcNow + KEY_DEFAULT_LIFETIME);
                allKeys = _keyManager.GetAllKeys().ToArray();
                defaultKey = FindDefaultKey(utcNow, allKeys);
            }

            // We really should have a default key at this point.
            CryptoUtil.Assert(defaultKey != null, "defaultKey != null");

            var cachedKeyRingHardRefreshTime = GetNextHardRefreshTime(utcNow);
            return new CachedKeyRing
            {
                KeyRing = new KeyRing(defaultKey.KeyId, allKeys),
                Keys = allKeys,
                HardRefreshTimeUtc = cachedKeyRingHardRefreshTime,
                SoftRefreshTimeUtc = MinDateTime(defaultKey.ExpirationDate.UtcDateTime, cachedKeyRingHardRefreshTime)
            };
        }

        private static IKey FindDefaultKey(DateTime utcNow, IKey[] allKeys)
        {
            bool unused;
            return FindDefaultKey(utcNow, allKeys, out unused);
        }

        private static IKey FindDefaultKey(DateTime utcNow, IKey[] allKeys, out bool callerShouldGenerateNewKey)
        {
            callerShouldGenerateNewKey = false;

            // Find the keys with the nearest past and future activation dates.
            IKey keyWithNearestPastActivationDate = null;
            IKey keyWithNearestFutureActivationDate = null;
            foreach (var candidateKey in allKeys)
            {
                // Revoked keys are never eligible candidates to be the default key.
                if (candidateKey.IsRevoked)
                {
                    continue;
                }

                if (candidateKey.ActivationDate.UtcDateTime <= utcNow)
                {
                    if (keyWithNearestPastActivationDate == null || keyWithNearestPastActivationDate.ActivationDate < candidateKey.ActivationDate)
                    {
                        keyWithNearestPastActivationDate = candidateKey;
                    }
                }
                else
                {
                    if (keyWithNearestFutureActivationDate == null || keyWithNearestFutureActivationDate.ActivationDate > candidateKey.ActivationDate)
                    {
                        keyWithNearestFutureActivationDate = candidateKey;
                    }
                }
            }

            // If the most recently activated key hasn't yet expired, use it as the default key.
            if (keyWithNearestPastActivationDate != null && !keyWithNearestPastActivationDate.IsExpired(utcNow))
            {
                // Additionally, if it's about to expire and there will be a gap in the keyring during which there
                // is no valid default encryption key, the caller should generate a new key with deferred activation.
                if (keyWithNearestPastActivationDate.ExpirationDate.UtcDateTime - utcNow <= KEY_EXPIRATION_BUFFER)
                {
                    if (keyWithNearestFutureActivationDate == null || keyWithNearestFutureActivationDate.ActivationDate > keyWithNearestPastActivationDate.ExpirationDate)
                    {
                        callerShouldGenerateNewKey = true;
                    }
                }

                return keyWithNearestPastActivationDate;
            }

            // Failing that, is any key due for imminent activation? If so, use it as the default key.
            // This allows us to account for clock skew when multiple servers touch the repository.
            if (keyWithNearestFutureActivationDate != null
                && (keyWithNearestFutureActivationDate.ActivationDate.UtcDateTime - utcNow) < MAX_SERVER_TO_SERVER_CLOCK_SKEW
                && !keyWithNearestFutureActivationDate.IsExpired(utcNow) /* sanity check: expiration can't occur before activation */)
            {
                return keyWithNearestFutureActivationDate;
            }

            // Otherwise, there's no default key.
            return null;
        }

        public IKeyRing GetCurrentKeyRing()
        {
            DateTime utcNow = DateTime.UtcNow;

            // Can we return the cached keyring to the caller?
            var existingCachedKeyRing = Volatile.Read(ref _cachedKeyRing);
            if (existingCachedKeyRing != null && existingCachedKeyRing.SoftRefreshTimeUtc > utcNow)
            {
                return existingCachedKeyRing.KeyRing;
            }

            // The cached keyring hasn't been created or must be refreshed.
            lock (_cachedKeyRingLockObj)
            {
                // Did somebody update the keyring while we were waiting for the lock?
                existingCachedKeyRing = Volatile.Read(ref _cachedKeyRing);
                if (existingCachedKeyRing != null && existingCachedKeyRing.SoftRefreshTimeUtc > utcNow)
                {
                    return existingCachedKeyRing.KeyRing;
                }

                // It's up to us to refresh the cached keyring.
                var newCachedKeyRing = CreateCachedKeyRingInstanceUnderLock(utcNow, existingCachedKeyRing);
                Volatile.Write(ref _cachedKeyRing, newCachedKeyRing);
                return newCachedKeyRing.KeyRing;
            }
        }

        private static DateTime GetNextHardRefreshTime(DateTime utcNow)
        {
            // We'll fudge the refresh period up to 20% so that multiple applications don't try to
            // hit a single repository simultaneously. For instance, if the refresh period is 1 hour,
            // we'll calculate the new refresh time as somewhere between 48 - 60 minutes from now.
            var skewedRefreshPeriod = TimeSpan.FromTicks((long)(KEYRING_REFRESH_PERIOD.Ticks * ((new Random().NextDouble() / 5) + 0.8d)));
            return utcNow + skewedRefreshPeriod;
        }

        private static DateTime MinDateTime(DateTime a, DateTime b)
        {
            Debug.Assert(a.Kind == DateTimeKind.Utc);
            Debug.Assert(b.Kind == DateTimeKind.Utc);
            return (a < b) ? a : b;
        }

        private sealed class CachedKeyRing
        {
            internal DateTime HardRefreshTimeUtc;
            internal KeyRing KeyRing;
            internal IKey[] Keys;
            internal DateTime SoftRefreshTimeUtc;
        }
    }
}
