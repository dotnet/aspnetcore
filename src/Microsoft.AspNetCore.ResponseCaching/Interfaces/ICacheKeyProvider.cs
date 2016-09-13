// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface ICacheKeyProvider
    {
        /// <summary>
        /// Create a base key for storing items.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns>The created base key.</returns>
        string CreateStorageBaseKey(ResponseCachingContext context);

        /// <summary>
        /// Create one or more base keys for looking up items.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns>An ordered <see cref="IEnumerable{T}"/> containing the base keys to try when looking up items.</returns>
        IEnumerable<string> CreateLookupBaseKeys(ResponseCachingContext context);

        /// <summary>
        /// Create a vary key for storing items.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns>The created vary key.</returns>
        string CreateStorageVaryKey(ResponseCachingContext context);

        /// <summary>
        /// Create one or more vary keys for looking up items.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns>An ordered <see cref="IEnumerable{T}"/> containing the vary keys to try when looking up items.</returns>
        IEnumerable<string> CreateLookupVaryKeys(ResponseCachingContext context);
    }
}
