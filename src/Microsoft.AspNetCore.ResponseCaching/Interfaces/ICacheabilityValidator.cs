// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface ICacheabilityValidator
    {
        /// <summary>
        /// Determine the cacheability of an HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if the request is cacheable; otherwise <c>false</c>.</returns>
        bool IsRequestCacheable(ResponseCachingContext context);

        /// <summary>
        /// Determine the cacheability of an HTTP response.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if the response is cacheable; otherwise <c>false</c>.</returns>
        bool IsResponseCacheable(ResponseCachingContext context);

        /// <summary>
        /// Determine the freshness of the cached entry.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if the cached entry is fresh; otherwise <c>false</c>.</returns>
        bool IsCachedEntryFresh(ResponseCachingContext context);
    }
}
