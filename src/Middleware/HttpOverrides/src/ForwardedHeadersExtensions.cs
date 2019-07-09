// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class ForwardedHeadersExtensions
    {
        private const string ForwardedHeadersAdded = "ForwardedHeadersAdded";

        /// <summary>
        /// Forwards proxied headers onto current request
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Don't add more than one instance of this middleware to the pipeline using the options from the DI container.
            // Doing so could cause a request to be processed multiple times and the ForwardLimit to be exceeded.
            if (!builder.Properties.ContainsKey(ForwardedHeadersAdded))
            {
                builder.Properties[ForwardedHeadersAdded] = true;
                return builder.UseMiddleware<ForwardedHeadersMiddleware>();
            }

            return builder;
        }

        /// <summary>
        /// Forwards proxied headers onto current request
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Enables the different forwarding options.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder builder, ForwardedHeadersOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseMiddleware<ForwardedHeadersMiddleware>(Options.Create(options));
        }
    }
}
