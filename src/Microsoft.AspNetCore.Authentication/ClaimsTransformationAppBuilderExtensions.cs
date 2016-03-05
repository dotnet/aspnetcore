// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
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
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ClaimsTransformationMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="ClaimsTransformationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables claims transformation capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="transform">A function that asynchronously transforms one <see cref="ClaimsPrincipal"/> to another.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, Func<ClaimsTransformationContext, Task<ClaimsPrincipal>> transform)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            return app.UseClaimsTransformation(new ClaimsTransformationOptions
            {
                Transformer = new ClaimsTransformer { OnTransform = transform }
            });
        }

        /// <summary>
        /// Adds the <see cref="ClaimsTransformationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables claims transformation capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">The <see cref="ClaimsTransformationOptions"/> to configure the middleware with.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, ClaimsTransformationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<ClaimsTransformationMiddleware>(Options.Create(options));
        }
    }
}
