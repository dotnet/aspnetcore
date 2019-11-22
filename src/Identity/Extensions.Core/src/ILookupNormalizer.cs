// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for normalizing keys (emails/names) for lookup purposes.
    /// </summary>
    public interface ILookupNormalizer
    {
        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The key to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="name"/>.</returns>
        string NormalizeName(string name);

        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="email"/>.
        /// </summary>
        /// <param name="email">The email to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="email"/>.</returns>
        string NormalizeEmail(string email);

    }
}
