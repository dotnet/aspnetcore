// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal static class InternalHttpContextExtensions
    {
        internal static void AddResponseCacheFeature(this HttpContext httpContext)
        {
            if (httpContext.GetResponseCacheFeature() != null)
            {
                throw new InvalidOperationException($"Another instance of {nameof(ResponseCacheFeature)} already exists. Only one instance of {nameof(ResponseCacheMiddleware)} can be configured for an application.");
            }
            httpContext.Features.Set(new ResponseCacheFeature());
        }

        internal static void RemoveResponseCacheFeature(this HttpContext httpContext)
        {
            httpContext.Features.Set<ResponseCacheFeature>(null);
        }
    }
}
