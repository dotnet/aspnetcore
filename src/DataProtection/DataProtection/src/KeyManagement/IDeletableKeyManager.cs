// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// An extension of <see cref="IKeyManager"/> that supports key deletion.
/// </summary>
public interface IDeletableKeyManager : IKeyManager
{
    /// <summary>
    /// Indicates whether this key manager and the underlying <see cref="Repositories.IXmlRepository"/> support key deletion.
    /// </summary>
    /// <seealso cref="DeleteKeys"/>
    bool CanDeleteKeys { get; }

    /// <summary>
    /// Deletes keys matching a predicate.
    ///
    /// Use with caution as deleting active keys will normally cause data loss.
    /// </summary>
    /// <param name="shouldDelete">
    /// A predicate applied to each key.
    /// Returning true will cause the key to be deleted.
    /// </param>
    /// <returns>
    /// True if all attempted deletions succeeded.
    /// </returns>
    /// <remarks>
    /// Deletion is stronger than revocation.  A revoked key is retained and can even be (forcefully) applied.
    /// A deleted key is indistinguishable from a key that never existed.
    /// 
    /// Generally, keys should only be deleted to save space.  If space is not a concern, keys
    /// should be revoked or allowed to expire instead.
    ///
    /// This method will not mutate existing IKey instances. After calling this method,
    /// all existing IKey instances should be discarded, and GetAllKeys should be called again.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// If <see cref="CanDeleteKeys"/> is false.
    /// </exception>
    bool DeleteKeys(Func<IKey, bool> shouldDelete);
}
