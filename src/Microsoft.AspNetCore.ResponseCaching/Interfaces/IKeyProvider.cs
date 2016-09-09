// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface IKeyProvider
    {
        /// <summary>
        /// Create a base key using the HTTP context for storing items.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>The created base key.</returns>
        string CreateStorageBaseKey(HttpContext httpContext);

        /// <summary>
        /// Create one or more base keys using the HTTP context for looking up items.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>An ordered <see cref="IEnumerable{T}"/> containing the base keys to try when looking up items.</returns>
        IEnumerable<string> CreateLookupBaseKey(HttpContext httpContext);

        /// <summary>
        /// Create a vary key using the HTTP context and vary rules for storing items.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <param name="varyRules">The <see cref="VaryRules"/>.</param>
        /// <returns>The created vary key.</returns>
        string CreateStorageVaryKey(HttpContext httpContext, VaryRules varyRules);

        /// <summary>
        /// Create one or more vary keys using the HTTP context and vary rules for looking up items.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <param name="varyRules">The <see cref="VaryRules"/>.</param>
        /// <returns>An ordered <see cref="IEnumerable{T}"/> containing the vary keys to try when looking up items.</returns>
        IEnumerable<string> CreateLookupVaryKey(HttpContext httpContext, VaryRules varyRules);
    }
}
