// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.HttpOverrides;
using Microsoft.Extensions.Options;

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
        public static IApplicationBuilder UseOverrideHeaders(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<OverrideHeaderMiddleware>();
        }

        /// <summary>
        /// Forwards proxied headers onto current request
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Enables the different override options.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseOverrideHeaders(this IApplicationBuilder builder, OverrideHeaderOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseMiddleware<OverrideHeaderMiddleware>(Options.Create(options));
        }
    }
}
