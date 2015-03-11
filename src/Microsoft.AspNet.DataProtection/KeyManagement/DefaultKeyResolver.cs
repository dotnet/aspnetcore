// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    /// <summary>
    /// Implements policy for resolving the default key from a candidate keyring.
    /// </summary>
    internal sealed class DefaultKeyResolver : IDefaultKeyResolver
    {
        /// <summary>
        /// The window of time before the key expires when a new key should be created
        /// and persisted to the keyring to ensure uninterrupted service.
        /// </summary>
        /// <remarks>
        /// If the expiration window is 5 days and the current key expires within 5 days,
        /// a new key will be generated.
        /// </remarks>
        private readonly TimeSpan _keyGenBeforeExpirationWindow;

        private readonly ILogger _logger;

        /// <summary>
        /// The maximum skew that is allowed between servers.
        /// This is used to allow newly-created keys to be used across servers even though
        /// their activation dates might be a few minutes into the future.
        /// </summary>
        /// <remarks>
        /// If the max skew is 5 minutes and the best matching candidate default key has
        /// an activation date of less than 5 minutes in the future, we'll use it.
        /// </remarks>
        private readonly TimeSpan _maxServerToServerClockSkew;

        public DefaultKeyResolver(TimeSpan keyGenBeforeExpirationWindow, TimeSpan maxServerToServerClockSkew, IServiceProvider services)
        {
            _keyGenBeforeExpirationWindow = keyGenBeforeExpirationWindow;
            _maxServerToServerClockSkew = maxServerToServerClockSkew;
            _logger = services.GetLogger<DefaultKeyResolver>();
        }

        public DefaultKeyResolution ResolveDefaultKeyPolicy(DateTimeOffset now, IEnumerable<IKey> allKeys)
        {
            DefaultKeyResolution retVal = default(DefaultKeyResolution);
            retVal.DefaultKey = FindDefaultKey(now, allKeys, out retVal.ShouldGenerateNewKey);
            return retVal;
        }

        private IKey FindDefaultKey(DateTimeOffset now, IEnumerable<IKey> allKeys, out bool callerShouldGenerateNewKey)
        {
            // the key with the most recent activation date where the activation date is in the past
            IKey keyMostRecentlyActivated = (from key in allKeys
                                             where key.ActivationDate <= now
                                             orderby key.ActivationDate descending
                                             select key).FirstOrDefault();

            if (keyMostRecentlyActivated != null)
            {
                if (_logger.IsVerboseLevelEnabled())
                {
                    _logger.LogVerbose("Considering key '{0:D}' with expiration date {1:u} as default key candidate.", keyMostRecentlyActivated.KeyId, keyMostRecentlyActivated.ExpirationDate);
                }

                // if the key has been revoked or is expired, it is no longer a candidate
                if (keyMostRecentlyActivated.IsExpired(now) || keyMostRecentlyActivated.IsRevoked)
                {
                    if (_logger.IsVerboseLevelEnabled())
                    {
                        _logger.LogVerbose("Key '{0:D}' no longer eligible as default key candidate because it is expired or revoked.", keyMostRecentlyActivated.KeyId);
                    }
                    keyMostRecentlyActivated = null;
                }
            }

            // There's an interesting edge case here. If two keys have an activation date in the past and
            // an expiration date in the future, and if the most recently activated of those two keys is
            // revoked, we won't consider the older key a valid candidate. This is intentional: generating
            // a new key is an implicit signal that we should stop using older keys without explicitly
            // revoking them.

            // if the key's expiration is beyond our safety window, we can use this key
            if (keyMostRecentlyActivated != null && keyMostRecentlyActivated.ExpirationDate - now > _keyGenBeforeExpirationWindow)
            {
                callerShouldGenerateNewKey = false;
                return keyMostRecentlyActivated;
            }

            // the key with the nearest activation date where the activation date is in the future
            // and the key isn't expired or revoked
            IKey keyNextPendingActivation = (from key in allKeys
                                             where key.ActivationDate > now && !key.IsExpired(now) && !key.IsRevoked
                                             orderby key.ActivationDate ascending
                                             select key).FirstOrDefault();

            // if we have a valid current key, return it, and signal to the caller that he must perform
            // the keygen step only if the next key pending activation won't be activated until *after*
            // the current key expires (allowing for server-to-server skew)
            if (keyMostRecentlyActivated != null)
            {
                callerShouldGenerateNewKey = (keyNextPendingActivation == null || (keyNextPendingActivation.ActivationDate - keyMostRecentlyActivated.ExpirationDate > _maxServerToServerClockSkew));
                if (callerShouldGenerateNewKey && _logger.IsVerboseLevelEnabled())
                {
                    _logger.LogVerbose("Default key expiration imminent and repository contains no viable successor. Caller should generate a successor.");
                }

                return keyMostRecentlyActivated;
            }

            // if there's no valid current key but there is a key pending activation, we can use
            // it only if its activation period is within the server-to-server clock skew
            if (keyNextPendingActivation != null && keyNextPendingActivation.ActivationDate - now <= _maxServerToServerClockSkew)
            {
                if (_logger.IsVerboseLevelEnabled())
                {
                    _logger.LogVerbose("Considering key '{0:D}' with expiration date {1:u} as default key candidate.", keyNextPendingActivation.KeyId, keyNextPendingActivation.ExpirationDate);
                }

                callerShouldGenerateNewKey = false;
                return keyNextPendingActivation;
            }

            // if we got this far, there was no valid default key in the keyring
            if (_logger.IsVerboseLevelEnabled())
            {
                _logger.LogVerbose("Repository contains no viable default key. Caller should generate a key with immediate activation.");
            }
            callerShouldGenerateNewKey = true;
            return null;
        }
    }
}
