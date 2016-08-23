// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface IResponseCachingCacheabilityValidator
    {
        /// <summary>
        /// Override default behavior for determining cacheability of an HTTP request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>The <see cref="OverrideResult"/>.</returns>
        OverrideResult RequestIsCacheableOverride(HttpContext httpContext);

        /// <summary>
        /// Override default behavior for determining cacheability of an HTTP response.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>The <see cref="OverrideResult"/>.</returns>
        OverrideResult ResponseIsCacheableOverride(HttpContext httpContext);
    }
}
