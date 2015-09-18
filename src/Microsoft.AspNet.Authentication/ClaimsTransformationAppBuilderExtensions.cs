// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods provided by the claims transformation authentication middleware
    /// </summary>
    public static class ClaimsTransformationAppBuilderExtensions
    {
        /// <summary>
        /// Adds a claims transformation middleware to your web application pipeline.
        /// </summary>
        /// <param name="options">The options for the middleware</param>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, ClaimsTransformationOptions options)
        {
            return app.UseMiddleware<ClaimsTransformationMiddleware>(options);
        }

        /// <summary>
        /// Adds a claims transformation middleware to your web application pipeline.
        /// </summary>
        /// <param name="options">The options for the middleware</param>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, Func<ClaimsPrincipal, Task<ClaimsPrincipal>> transform)
        {
            var options = new ClaimsTransformationOptions();
            options.Transformer = new ClaimsTransformer
            {
                OnTransform = transform
            };
            return app.UseClaimsTransformation(options);
        }

        /// <summary>
        /// Adds a claims transformation middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <param name="configureOptions">Used to configure the options for the middleware</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, Action<ClaimsTransformationOptions> configureOptions)
        {
            var options = new ClaimsTransformationOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseClaimsTransformation(options);
        }
    }
}