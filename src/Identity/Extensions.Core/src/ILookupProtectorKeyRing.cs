// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Abstraction used to manage named keys used to protect lookups.
    /// </summary>
    public interface ILookupProtectorKeyRing
    {
        /// <summary>
        /// Get the current key id.
        /// </summary>
        string CurrentKeyId { get; }

        /// <summary>
        /// Return a specific key.
        /// </summary>
        /// <param name="keyId">The id of the key to fetch.</param>
        /// <returns>The key ring.</returns>
        string this[string keyId] { get; }

        /// <summary>
        /// Return all of the key ids.
        /// </summary>
        /// <returns>All of the key ids.</returns>
        IEnumerable<string> GetAllKeyIds();
    }
}