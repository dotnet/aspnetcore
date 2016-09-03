// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal static class HttpContextInternalExtensions
    {
        internal static void AddResponseCachingFeature(this HttpContext httpContext)
        {
            if (httpContext.GetResponseCachingFeature() != null)
            {
                throw new InvalidOperationException($"Another instance of {nameof(ResponseCachingFeature)} already exists. Only one instance of {nameof(ResponseCachingMiddleware)} can be configured for an application.");
            }
            httpContext.Features.Set(new ResponseCachingFeature());
        }

        internal static void RemoveResponseCachingFeature(this HttpContext httpContext)
        {
            httpContext.Features.Set<ResponseCachingFeature>(null);
        }

        internal static void AddResponseCachingState(this HttpContext httpContext)
        {
            if (httpContext.GetResponseCachingState() != null)
            {
                throw new InvalidOperationException($"Another instance of {nameof(ResponseCachingState)} already exists. Only one instance of {nameof(ResponseCachingMiddleware)} can be configured for an application.");
            }
            httpContext.Features.Set(new ResponseCachingState(httpContext));
        }

        internal static void RemoveResponseCachingState(this HttpContext httpContext)
        {
            httpContext.Features.Set<ResponseCachingState>(null);
        }

        internal static ResponseCachingState GetResponseCachingState(this HttpContext httpContext)
        {
            return httpContext.Features.Get<ResponseCachingState>();
        }
    }
}
