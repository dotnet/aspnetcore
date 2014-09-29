// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Security.DataProtection.KeyManagement
{
    /// <summary>
    /// The basic interface for performing key management operations.
    /// </summary>
    public interface IKeyManager
    {
        /// <summary>
        /// Creates a new key with the specified activation and expiration dates.
        /// </summary>
        /// <param name="activationDate">The date on which encryptions to this key may begin.</param>
        /// <param name="expirationDate">The date after which encryptions to this key may no longer take place.</param>
        /// <returns>The newly-created IKey instance.</returns>
        /// <remarks>
        /// This method also persists the newly-created IKey instance to the underlying repository.
        /// </remarks>
        IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate);

        /// <summary>
        /// Fetches all keys from the underlying repository.
        /// </summary>
        /// <returns>The collection of all keys.</returns>
        IReadOnlyCollection<IKey> GetAllKeys();

        /// <summary>
        /// Revokes a specific key.
        /// </summary>
        /// <param name="keyId">The id of the key to revoke.</param>
        /// <param name="reason">An optional human-readable reason for revocation.</param>
        /// <remarks>
        /// This method will not mutate existing IKey instances. After calling this method,
        /// all existing IKey instances should be discarded, and GetAllKeys should be called again.
        /// </remarks>
        void RevokeKey(Guid keyId, string reason = null);

        /// <summary>
        /// Revokes all keys created before a specified date.
        /// </summary>
        /// <param name="revocationDate">The revocation date. All keys with a creation date before
        /// this value will be revoked.</param>
        /// <param name="reason">An optional human-readable reason for revocation.</param>
        /// <remarks>
        /// This method will not mutate existing IKey instances. After calling this method,
        /// all existing IKey instances should be discarded, and GetAllKeys should be called again.
        /// </remarks>
        void RevokeAllKeys(DateTimeOffset revocationDate, string reason = null);
    }
}
