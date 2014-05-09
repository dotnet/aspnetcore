// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder.Extensions;

namespace Microsoft.AspNet.Builder
{
    public static class MapExtensions
    {
        /// <summary>
        /// If the request path starts with the given pathMatch, execute the app configured via configuration parameter instead of
        /// continuing to the next component in the pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="pathMatch">The path to match</param>
        /// <param name="configuration">The branch to take for positive path matches</param>
        /// <returns></returns>
        public static IBuilder Map([NotNull] this IBuilder app, [NotNull] string pathMatch, [NotNull] Action<IBuilder> configuration)
        {
            return Map(app, new PathString(pathMatch), configuration);
        }

        /// <summary>
        /// If the request path starts with the given pathMatch, execute the app configured via configuration parameter instead of
        /// continuing to the next component in the pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="pathMatch">The path to match</param>
        /// <param name="configuration">The branch to take for positive path matches</param>
        /// <returns></returns>
        public static IBuilder Map([NotNull] this IBuilder app, PathString pathMatch, [NotNull] Action<IBuilder> configuration)
        {
            if (pathMatch.HasValue && pathMatch.Value.EndsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("The path must not end with a '/'", "pathMatch");
            }

            // create branch
            IBuilder branchBuilder = app.New();
            configuration(branchBuilder);
            var branch = branchBuilder.Build();

            var options = new MapOptions()
            {
                Branch = branch,
                PathMatch = pathMatch,
            };
            return app.Use(next => new MapMiddleware(next, options).Invoke);
        }
    }
}