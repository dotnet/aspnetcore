// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// The basic interface for performing key management operations.
/// </summary>
/// <remarks>
/// Instantiations of this interface are expected to be thread-safe.
/// </remarks>
public interface IKeyManager
{
    /// <summary>
    /// Creates a new key with the specified activation and expiration dates and persists
    /// the new key to the underlying repository.
    /// </summary>
    /// <param name="activationDate">The date on which encryptions to this key may begin.</param>
    /// <param name="expirationDate">The date after which encryptions to this key may no longer take place.</param>
    /// <returns>The newly-created IKey instance.</returns>
    IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate);

    /// <summary>
    /// Fetches all keys from the underlying repository.
    /// </summary>
    /// <returns>The collection of all keys.</returns>
    IReadOnlyCollection<IKey> GetAllKeys();

    /// <summary>
    /// Retrieves a token that signals that callers who have cached the return value of
    /// GetAllKeys should clear their caches. This could be in response to a call to
    /// CreateNewKey or RevokeKey, or it could be in response to some other external notification.
    /// Callers who are interested in observing this token should call this method before the
    /// corresponding call to GetAllKeys.
    /// </summary>
    /// <returns>
    /// The cache expiration token. When an expiration notification is triggered, any
    /// tokens previously returned by this method will become canceled, and tokens returned by
    /// future invocations of this method will themselves not trigger until the next expiration
    /// event.
    /// </returns>
    /// <remarks>
    /// Implementations are free to return 'CancellationToken.None' from this method.
    /// Since this token is never guaranteed to fire, callers should still manually
    /// clear their caches at a regular interval.
    /// </remarks>
    CancellationToken GetCacheExpirationToken();

    /// <summary>
    /// Revokes a specific key and persists the revocation to the underlying repository.
    /// </summary>
    /// <param name="keyId">The id of the key to revoke.</param>
    /// <param name="reason">An optional human-readable reason for revocation.</param>
    /// <remarks>
    /// This method will not mutate existing IKey instances. After calling this method,
    /// all existing IKey instances should be discarded, and GetAllKeys should be called again.
    /// </remarks>
    void RevokeKey(Guid keyId, string? reason = null);

    /// <summary>
    /// Revokes all keys created before a specified date and persists the revocation to the
    /// underlying repository.
    /// </summary>
    /// <param name="revocationDate">The revocation date. All keys with a creation date before
    /// this value will be revoked.</param>
    /// <param name="reason">An optional human-readable reason for revocation.</param>
    /// <remarks>
    /// This method will not mutate existing IKey instances. After calling this method,
    /// all existing IKey instances should be discarded, and GetAllKeys should be called again.
    /// </remarks>
    void RevokeAllKeys(DateTimeOffset revocationDate, string? reason = null);

#if NETCOREAPP
    /// <summary>
    /// Indicates whether this key manager supports key deletion.
    /// </summary>
    /// <remarks>
    /// Deletion is stronger than revocation.  A revoked key is retained and can even be (forcefully) applied.
    /// A deleted key is indistinguishable from a key that never existed.
    /// </remarks>
    bool CanDeleteKeys => false;

    /// <summary>
    /// Deletes keys matching a predicate.
    /// </summary>
    /// <param name="shouldDelete">
    /// A predicate applied to each expired key (and unexpired key if <paramref name="unsafeIncludeUnexpired"/> is true).
    /// Returning true will cause the key to be deleted.
    /// </param>
    /// <param name="unsafeIncludeUnexpired">
    /// True indicates that unexpired keys (which may be presently or yet to be activated) should also be
    /// passed to <paramref name="shouldDelete"/>.
    /// Use with caution as deleting active keys will normally cause data loss.
    /// </param>
    /// <remarks>
    /// Generally, keys should only be deleted to save space.  If space is not a concern, keys
    /// should be revoked or allowed to expire instead.
    /// 
    /// This method will not mutate existing IKey instances. After calling this method,
    /// all existing IKey instances should be discarded, and GetAllKeys should be called again.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// If <see cref="CanDeleteKeys"/> is false.
    /// </exception>
    void DeleteKeys(Func<IKey, bool> shouldDelete, bool unsafeIncludeUnexpired = false) => throw new NotSupportedException();
#endif
}
