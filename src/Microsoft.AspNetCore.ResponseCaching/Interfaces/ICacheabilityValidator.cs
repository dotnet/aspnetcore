// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface ICacheabilityValidator
    {
        /// <summary>
        /// Determine the cacheability of an HTTP request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns><c>true</c> if the request is cacheable; otherwise <c>false</c>.</returns>
        bool RequestIsCacheable(HttpContext httpContext);

        /// <summary>
        /// Determine the cacheability of an HTTP response.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns><c>true</c> if the response is cacheable; otherwise <c>false</c>.</returns>
        bool ResponseIsCacheable(HttpContext httpContext);

        /// <summary>
        /// Determine the freshness of the cached entry.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <param name="cachedResponseHeaders">The <see cref="ResponseHeaders"/> of the cached entry.</param>
        /// <returns><c>true</c> if the cached entry is fresh; otherwise <c>false</c>.</returns>
        bool CachedEntryIsFresh(HttpContext httpContext, ResponseHeaders cachedResponseHeaders);
    }
}
