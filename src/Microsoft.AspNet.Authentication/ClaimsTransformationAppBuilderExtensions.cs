// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add claims transformation capabilities to an HTTP application pipeline.
    /// </summary>
    public static class ClaimsTransformationAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="ClaimsTransformationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables claims transformation capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="ClaimsTransformationOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, ClaimsTransformationOptions options)
        {
            return app.UseMiddleware<ClaimsTransformationMiddleware>(options);
        }

        /// <summary>
        /// Adds the <see cref="ClaimsTransformationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables claims transformation capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="transform">A function that asynchronously transforms one <see cref="ClaimsPrincipal"/> to another.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
        /// Adds the <see cref="ClaimsTransformationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables claims transformation capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="ClaimsTransformationOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
