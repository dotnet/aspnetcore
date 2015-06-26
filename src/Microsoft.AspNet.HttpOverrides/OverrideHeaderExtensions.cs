// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.HttpOverrides;

namespace Microsoft.AspNet.Builder
{
    public static class OverrideHeaderExtensions
    {
        /// <summary>
        /// Forwards proxied headers onto current request
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Enables the different override options.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseOverrideHeaders(this IApplicationBuilder builder, OverrideHeaderMiddlewareOptions options)
        {
            return builder.Use(next => new OverrideHeaderMiddleware(next, options).Invoke);
        }
    }
}
