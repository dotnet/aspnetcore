// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for normalizing keys for lookup purposes.
    /// </summary>
    public interface ILookupNormalizer
    {
        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="key"/>.</returns>
        string Normalize(string key);
    }
}