// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching
{
    // TODO: Temporary interface for endpoints to specify options for response caching
    public static class ResponseCachingHttpContextExtensions
    {
        public static void AddResponseCachingFeature(this HttpContext httpContext)
        {
            httpContext.Features.Set(new ResponseCachingFeature());
        }

        public static void RemoveResponseCachingFeature(this HttpContext httpContext)
        {
            httpContext.Features.Set<ResponseCachingFeature>(null);
        }

        public static ResponseCachingFeature GetResponseCachingFeature(this HttpContext httpContext)
        {
            return httpContext.Features.Get<ResponseCachingFeature>() ?? new ResponseCachingFeature();
        }
    }
}
