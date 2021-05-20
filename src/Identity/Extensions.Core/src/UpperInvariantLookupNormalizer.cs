// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Implements <see cref="ILookupNormalizer"/> by converting keys to their upper cased invariant culture representation.
    /// </summary>
    public sealed class UpperInvariantLookupNormalizer : ILookupNormalizer
    {
        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The key to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="name"/>.</returns>
        public string NormalizeName(string name)
        {
            if (name == null)
            {
                return null;
            }
            return name.Normalize().ToUpperInvariant();
        }

        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="email"/>.
        /// </summary>
        /// <param name="email">The email to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="email"/>.</returns>
        public string NormalizeEmail(string email) => NormalizeName(email);
    }
}
