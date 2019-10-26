// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions
{
    /// <summary>
    /// Represents a middleware that maps a request path to a sub-request pipeline.
    /// </summary>
    public class MapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MapOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="MapMiddleware"/>.
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

            if (context.Request.Path.StartsWithSegments(_options.PathMatch, out var matchedPath, out var remainingPath))
            {
                var path = context.Request.Path;
                var pathBase = context.Request.PathBase;

                if (!_options.PreserveMatchedPathSegment)
                {
                    // Update the path
                    context.Request.PathBase = pathBase.Add(matchedPath);
                    context.Request.Path = remainingPath;
                }

                try
                {
                    await _options.Branch(context);
                }
                finally
                {
                    if (!_options.PreserveMatchedPathSegment)
                    {
                        context.Request.PathBase = pathBase;
                        context.Request.Path = path;
                    }
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
