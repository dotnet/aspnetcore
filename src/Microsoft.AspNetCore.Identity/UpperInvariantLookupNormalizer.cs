// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Implements <see cref="ILookupNormalizer"/> by converting keys to their upper cased invariant culture representation.
    /// </summary>
    public class UpperInvariantLookupNormalizer : ILookupNormalizer
    {
        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="key"/>
        /// by converting keys to their upper cased invariant culture representation.
        /// </summary>
        /// <param name="key">The key to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="key"/>.</returns>
        public virtual string Normalize(string key)
        {
            if (key == null)
            {
                return null;
            }
            return key.Normalize().ToUpperInvariant();
        }
    }
}
