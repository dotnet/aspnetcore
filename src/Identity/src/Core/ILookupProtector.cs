// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Used to protect/unprotect lookups with a specific key.
    /// </summary>
    public interface ILookupProtector
    {
        /// <summary>
        /// Protect the data using the specified key.
        /// </summary>
        /// <param name="keyId">The key to use.</param>
        /// <param name="data">The data to protect.</param>
        /// <returns>The protected data.</returns>
        string Protect(string keyId, string data);

        /// <summary>
        /// Unprotect the data using the specified key.
        /// </summary>
        /// <param name="keyId">The key to use.</param>
        /// <param name="data">The data to unprotect.</param>
        /// <returns>The original data.</returns>
        string Unprotect(string keyId, string data);
    }
}