// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder.Extensions
{
    /// <summary>
    /// Respresents a middleware that maps a request path to a sub-request pipeline.
    /// </summary>
    public class MapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MapOptions _options;

        /// <summary>
        /// Creates a new instace of <see cref="MapMiddleware"/>.
        /// </summary>
        /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
        /// <param name="options">The middleware options.</param>
        public MapMiddleware(RequestDelegate next, MapOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options;
        }

        /// <summary>
        /// Executes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the execution of this middleware.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            PathString path = context.Request.Path;
            PathString remainingPath;
            if (path.StartsWithSegments(_options.PathMatch, out remainingPath))
            {
                // Update the path
                PathString pathBase = context.Request.PathBase;
                context.Request.PathBase = pathBase + _options.PathMatch;
                context.Request.Path = remainingPath;

                try
                {
                    await _options.Branch(context);
                }
                finally
                {
                    context.Request.PathBase = pathBase;
                    context.Request.Path = path;
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}