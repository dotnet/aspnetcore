// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder.Extensions;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the <see cref="MapMiddleware"/>.
    /// </summary>
    public static class MapExtensions
    {
        /// <summary>
        /// Branches the request pipeline based on matches of the given request path. If the request path starts with
        /// the given path, the branch is executed.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="pathMatch">The request path to match.</param>
        /// <param name="configuration">The branch to take for positive path matches.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString pathMatch, Action<IApplicationBuilder> configuration)
        {
            return Map(app, pathMatch, preserveMatchedPathSegment: false, configuration);
        }

        /// <summary>
        /// Branches the request pipeline based on matches of the given request path. If the request path starts with
        /// the given path, the branch is executed.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="pathMatch">The request path to match.</param>
        /// <param name="preserveMatchedPathSegment">if false, matched path would be removed from Request.Path and added to Request.PathBase.</param>
        /// <param name="configuration">The branch to take for positive path matches.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString pathMatch, bool preserveMatchedPathSegment, Action<IApplicationBuilder> configuration)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (pathMatch.HasValue && pathMatch.Value.EndsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("The path must not end with a '/'", nameof(pathMatch));
            }

            // create branch
            var branchBuilder = app.New();
            configuration(branchBuilder);
            var branch = branchBuilder.Build();

            var options = new MapOptions
            {
                Branch = branch,
                PathMatch = pathMatch,
                PreserveMatchedPathSegment = preserveMatchedPathSegment
            };
            return app.Use(next => new MapMiddleware(next, options).Invoke);
        }
    }
}
