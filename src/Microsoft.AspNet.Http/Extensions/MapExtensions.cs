// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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