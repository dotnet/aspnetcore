// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public interface IResponseCachePolicyProvider
    {
        /// <summary>
        /// Determine wehther the response cache middleware should be executed for the incoming HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCacheContext"/>.</param>
        /// <returns><c>true</c> if the request is cacheable; otherwise <c>false</c>.</returns>
        bool IsRequestCacheable(ResponseCacheContext context);

        /// <summary>
        /// Determine whether the response received by the middleware be cached for future requests.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCacheContext"/>.</param>
        /// <returns><c>true</c> if the response is cacheable; otherwise <c>false</c>.</returns>
        bool IsResponseCacheable(ResponseCacheContext context);

        /// <summary>
        /// Determine whether the response retrieved from the response cache is fresh and be served.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCacheContext"/>.</param>
        /// <returns><c>true</c> if the cached entry is fresh; otherwise <c>false</c>.</returns>
        bool IsCachedEntryFresh(ResponseCacheContext context);
    }
}
