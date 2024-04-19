// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// Implements policy for resolving the default key from a candidate keyring.
/// </summary>
internal sealed class DefaultKeyResolver : IDefaultKeyResolver
{
#if NETFRAMEWORK
    // Sadly, numerical AppContext data support was not added until 4.7 and we target 4.6
    private const string MaximumTotalDefaultKeyResolverRetriesSwitchKey = "Microsoft.AspNetCore.DataProtection.KeyManagement.DisableDefaultKeyResolverRetries";
#else
    private const string MaximumTotalDefaultKeyResolverRetriesDataKey = "Microsoft.AspNetCore.DataProtection.KeyManagement.MaximumTotalDefaultKeyResolverRetries";
#endif

    /// <summary>
    /// The window of time before the key expires when a new key should be created
    /// and persisted to the keyring to ensure uninterrupted service.
    /// </summary>
    /// <remarks>
    /// If the propagation time is 5 days and the current key expires within 5 days,
    /// a new key will be generated.
    /// </remarks>
    private readonly TimeSpan _keyPropagationWindow;

    private readonly int _maxDecryptRetries;
    private readonly TimeSpan _decryptRetryDelay;

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

    public DefaultKeyResolver(IOptions<KeyManagementOptions> keyManagementOptions)
        : this(keyManagementOptions, NullLoggerFactory.Instance)
    { }

    public DefaultKeyResolver(IOptions<KeyManagementOptions> keyManagementOptions, ILoggerFactory loggerFactory)
    {
        _keyPropagationWindow = KeyManagementOptions.KeyPropagationWindow;
        _maxServerToServerClockSkew = KeyManagementOptions.MaxServerClockSkew;
        _maxDecryptRetries = GetMaxDecryptRetriesFromAppContext() ?? keyManagementOptions.Value.MaximumTotalDefaultKeyResolverRetries;
        _decryptRetryDelay = keyManagementOptions.Value.DefaultKeyResolverRetryDelay;
        _logger = loggerFactory.CreateLogger<DefaultKeyResolver>();
    }

    private static int? GetMaxDecryptRetriesFromAppContext()
    {
#if NETFRAMEWORK
        // Force to zero if retries are disabled.  Otherwise, use the configured value
        return AppContext.TryGetSwitch(MaximumTotalDefaultKeyResolverRetriesSwitchKey, out var areRetriesDisabled) && areRetriesDisabled ? 0 : null;
#else
        var data = AppContext.GetData(MaximumTotalDefaultKeyResolverRetriesDataKey);

        // Programmatically-configured values are usually ints
        if (data is int count)
        {
            return count;
        }

        // msbuild-configured values are usually strings
        if (data is string countStr && int.TryParse(countStr, out var parsed))
        {
            return parsed;
        }

        return null;
#endif
    }

    private bool CanCreateAuthenticatedEncryptor(IKey key, ref int retriesRemaining)
    {
        List<Exception>? exceptions = null;
        while (true)
        {
            try
            {
                var encryptorInstance = key.CreateEncryptor();
                if (encryptorInstance is null)
                {
                    // This generally indicates a key (descriptor) without a corresponding IAuthenticatedEncryptorFactory,
                    // which is not something that can succeed on retry - return immediately
                    _logger.KeyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed(
                        key.KeyId,
                        nameof(IKey.CreateEncryptor),
                        new InvalidOperationException($"{nameof(IKey.CreateEncryptor)} returned null."));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (retriesRemaining > 0)
                {
                    if (exceptions is null)
                    {
                        // The first failure doesn't count as a retry
                        exceptions = [];
                    }
                    else
                    {
                        retriesRemaining--;
                    }

                    exceptions.Add(ex);
                }

                if (retriesRemaining <= 0) // Guard against infinite loops from programmer errors
                {
                    _logger.KeyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed(key.KeyId, nameof(IKey.CreateEncryptor), exceptions is null ? ex : new AggregateException(exceptions));
                    return false;
                }

                _logger.RetryingMethodOfKeyAfterFailure(key.KeyId, nameof(IKey.CreateEncryptor), ex);
            }

            // Reset the descriptor to allow for a retry
            (key as Key)?.ResetDescriptor();

            // Don't retry immediately - allow a little time for the transient problem to clear
            Thread.Sleep(_decryptRetryDelay);
        }
    }

    private IKey? FindDefaultKey(DateTimeOffset now, IEnumerable<IKey> allKeys, out IKey? fallbackKey)
    {
        // Keys created before this time should have propagated to all instances.
        var propagationCutoff = now - _keyPropagationWindow;

        // Prefer the most recently activated key that's old enough to have propagated to all instances.
        // If no such key exists, fall back to the *least* recently activated key that's too new to have
        // propagated to all instances.

        // An unpropagated key can still be preferred insofar as we wouldn't want to generate a replacement
        // for it (as the replacement would also be unpropagated).

        // Note that the two sort orders are opposite: we want the *newest* key that's old enough
        // (to have been propagated) or the *oldest* key that's too new.
        var activatedKeys = allKeys.Where(key => key.ActivationDate <= now + _maxServerToServerClockSkew);
        var preferredDefaultKey = (from key in activatedKeys
                                   where key.CreationDate <= propagationCutoff
                                   orderby key.ActivationDate descending, key.KeyId ascending
                                   select key).Concat(from key in activatedKeys
                                                      where key.CreationDate > propagationCutoff
                                                      orderby key.ActivationDate ascending, key.KeyId ascending
                                                      select key).FirstOrDefault();

        var decryptRetriesRemaining = _maxDecryptRetries;

        if (preferredDefaultKey != null)
        {
            _logger.ConsideringKeyWithExpirationDateAsDefaultKey(preferredDefaultKey.KeyId, preferredDefaultKey.ExpirationDate);

            // if the key has been revoked or is expired, it is no longer a candidate
            if (preferredDefaultKey.IsRevoked || preferredDefaultKey.IsExpired(now) || !CanCreateAuthenticatedEncryptor(preferredDefaultKey, ref decryptRetriesRemaining))
            {
                _logger.KeyIsNoLongerUnderConsiderationAsDefault(preferredDefaultKey.KeyId);
            }
            else
            {
                fallbackKey = null;
                return preferredDefaultKey;
            }
        }

        // If we got this far, the caller must generate a key now.
        // We should locate a fallback key, which is a key that can be used to protect payloads if
        // the caller is configured not to generate a new key. We should try to make sure the fallback
        // key has propagated to all callers (so its creation date should be before the previous
        // propagation period), and we cannot use revoked keys. The fallback key may be expired.

        // As above, the two sort orders are opposite.

        // Unlike for the preferred key, we don't choose a fallback key and then reject it if
        // CanCreateAuthenticatedEncryptor is false.  We want to end up with *some* key, so we
        // keep trying until we find one that works.
        var unrevokedKeys = allKeys.Where(key => !key.IsRevoked);
        fallbackKey = (from key in (from key in unrevokedKeys
                                    where !ReferenceEquals(key, preferredDefaultKey) // Don't reconsider it as a fallback
                                    where key.CreationDate <= propagationCutoff
                                    orderby key.CreationDate descending
                                    select key).Concat(from key in unrevokedKeys
                                                       where key.CreationDate > propagationCutoff
                                                       orderby key.CreationDate ascending
                                                       select key)
                       where CanCreateAuthenticatedEncryptor(key, ref decryptRetriesRemaining)
                       select key).FirstOrDefault();

        _logger.RepositoryContainsNoViableDefaultKey();

        return null;
    }

    public DefaultKeyResolution ResolveDefaultKeyPolicy(DateTimeOffset now, IEnumerable<IKey> allKeys)
    {
        var retVal = default(DefaultKeyResolution);
        var defaultKey = FindDefaultKey(now, allKeys, out retVal.FallbackKey);
        retVal.DefaultKey = defaultKey;
        retVal.ShouldGenerateNewKey = defaultKey is null;
        return retVal;
    }
}
